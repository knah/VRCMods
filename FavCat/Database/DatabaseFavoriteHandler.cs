using System;
using System.Collections.Generic;
using System.Linq;
using FavCat.Database.Stored;
using LiteDB;

namespace FavCat.Database
{
    public class DatabaseFavoriteHandler<T> where T: class
    {
        public readonly DatabaseEntity EntityType;
        internal readonly ILiteCollection<StoredFavorite> myStoredFavorites;
        private readonly ILiteCollection<StoredCategory> myStoredCategories;
        private readonly ILiteCollection<T> myObjectStore;
        private readonly ILiteCollection<StoredCategoryOrder> myStoredOrders;

        public event Action<string>? OnCategoryContentsChanged;

        public DatabaseFavoriteHandler(LiteDatabase database, DatabaseEntity entityType, ILiteCollection<T> objectStore,
            ILiteCollection<StoredCategoryOrder> storedOrders)
        {
            this.EntityType = entityType;
            myObjectStore = objectStore;
            myStoredOrders = storedOrders;

            var entityName = entityType.ToString();
            myStoredFavorites = database.GetCollection<StoredFavorite>($"{entityName}_favorites");
            myStoredCategories = database.GetCollection<StoredCategory>($"{entityName}_categories");

            myStoredFavorites.EnsureIndex("ObjectAndCategory", it => it.ObjectId + it.Category, true);
            
            myStoredFavorites.EnsureIndex(it => it.ObjectId);
            myStoredFavorites.EnsureIndex(it => it.Category);
        }

        public StoredCategoryOrder GetStoredOrder()
        {
            return myStoredOrders.FindById(EntityType.ToString()) ?? new StoredCategoryOrder {EntityType = EntityType};
        }

        public void SetStoredOrder(List<CategoryInfo> order, List<string> defaultListsToHide)
        {
            myStoredOrders.Upsert(new StoredCategoryOrder {EntityType = EntityType, Order = order, DefaultListsToHide = defaultListsToHide});
        }

        public void AddFavorite(string objectId, string category)
        {
            myStoredFavorites.Upsert(new StoredFavorite {AddedOn = DateTime.UtcNow, Category = category, ObjectId = objectId});
            OnCategoryContentsChanged?.Invoke(category);
        }

        public IEnumerable<(StoredFavorite, T)> ListFavorites(string category)
        {
            return myStoredFavorites.Find(it => it.Category == category).Select(it => (it, myObjectStore.FindById(it.ObjectId))).Where(it => it.Item2 != null);
        }

        private StoredFavorite? GetFavorite(string objectId, string category)
        {
            var target = objectId + category;
            return myStoredFavorites.FindOne(it => it.ObjectId + it.Category == target);
        }

        public bool IsFavorite(string objectId, string category)
        {
            return GetFavorite(objectId, category) != null;
        }

        public StoredCategory? GetCategory(string category)
        {
            return myStoredCategories.FindById(category);
        }

        public void UpdateCategory(StoredCategory category)
        {
            myStoredCategories.Upsert(category);
        }

        public void DeleteCategory(StoredCategory category)
        {
            myStoredFavorites.DeleteMany(it => it.Category == category.CategoryName);
            myStoredCategories.Delete(category.CategoryName);
        }

        public IEnumerable<StoredCategory> GetCategories()
        {
            return myStoredCategories.FindAll();
        }

        public void DeleteFavoriteFromAllCategories(string id)
        {
            var favs = myStoredFavorites.Find(it => it.ObjectId == id).ToList();
            foreach (var storedFavorite in favs) 
                myStoredFavorites.Delete(storedFavorite.FavoriteId);
            
            foreach (var affectedCategory in favs.Select(it => it.Category).Distinct())
                OnCategoryContentsChanged?.Invoke(affectedCategory);
        }

        public void DeleteFavorite(string id, string categoryName)
        {
            var favorite = GetFavorite(id, categoryName);
            if (favorite == null) return;
            myStoredFavorites.Delete(favorite.FavoriteId);
            OnCategoryContentsChanged?.Invoke(categoryName);
        }

        public void RenameCategory(StoredCategory category, string newName)
        {
            var oldName = category.CategoryName;
            myStoredCategories.Delete(oldName);
            foreach (var storedFavorite in myStoredFavorites.Find(it => it.Category == oldName))
            {
                storedFavorite.Category = newName;
                myStoredFavorites.Update(storedFavorite);
            }

            category.CategoryName = newName;
            myStoredCategories.Upsert(category);
            
            // no events fired
        }
    }
}