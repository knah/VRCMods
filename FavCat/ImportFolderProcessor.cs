using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FavCat.Database;
using FavCat.Database.Stored;
using FavCat.Modules;
using LiteDB;
using MelonLoader;
using UIExpansionKit.API;
using VRC.Core;
using Random = UnityEngine.Random;

namespace FavCat
{
    public static class ImportFolderProcessor
    {
        public static bool ImportRunning { get; private set; }

        public static string ImportStatusOuter { get; private set; } = "Not importing";
        public static string ImportStatusInner { get; private set; } = "";
        
        public static async Task ProcessImportsFolder()
        {
            ImportRunning = true;
            ImportStatusOuter = "Import running...";
            
            var databases = new List<string>();
            var textFiles = new List<string>();
            foreach (var file in Directory.EnumerateFiles("./UserData/FavCatImport"))
            {
                if (file.EndsWith(".db")) 
                    databases.Add(file);
                else
                    textFiles.Add(file);
            }

            for (var i = 0; i < databases.Count; i++)
            {
                var file = databases[i];
                try
                {
                    ImportStatusOuter = $"Importing database {i + 1}/{databases.Count}";
                    await MergeInForeignStore(file);
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    MelonLogger.Msg($"Import of {file} failed: {ex}");
                }
            }
            
            for (var i = 0; i < textFiles.Count; i++)
            {
                var file = textFiles[i];
                try
                {
                    ImportStatusOuter = $"Importing text file {i + 1}/{textFiles.Count}";
                    await ProcessTextFile(file);
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    MelonLogger.Msg($"Import of {file} failed: {ex}");
                }
            }

            ImportRunning = false;
        }
        
        private static readonly Regex ourUserIdRegex = new Regex("usr_[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}");
        private static readonly Regex ourWorldIdRegex = new Regex("wrld_[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}");

        // might be re-used for worlds/players
        internal static async Task ProcessTextFile(string filePath)
        {
            var database = FavCatMod.Database;
            if (database == null)
            {
                MelonLogger.Msg("Database does not exist, can't import");
                return;
            }
            
            var fileName = Path.GetFileName(filePath);
            MelonLogger.Msg($"Started avatar import process for file {fileName}");
            
            var toAddUsers = new List<string>();
            var toAddWorlds = new List<string>();
            { // file access block
                using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(file);
                string line;
                
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    foreach (Match match in ourUserIdRegex.Matches(line)) toAddUsers.Add(match.Value);
                    foreach (Match match in ourWorldIdRegex.Matches(line)) toAddWorlds.Add(match.Value);
                }
            }

            for (var i = 0; i < toAddUsers.Count; i++)
            {
                ImportStatusInner = $"Fetching user {i + 1}/{toAddUsers.Count}";
                var userId = toAddUsers[i];
                
                if (database.myStoredPlayers.FindById(userId) == null)
                {
                    await TaskUtilities.YieldToMainThread();
                    new APIUser {id = userId}.Fetch(); // it will get intercepted and stored
                    await Task.Delay(TimeSpan.FromSeconds(5f + Random.Range(0f, 5f))).ConfigureAwait(false);
                }
            }
            
            for (var i = 0; i < toAddWorlds.Count; i++)
            {
                ImportStatusInner = $"Fetching world {i + 1}/{toAddWorlds.Count}";
                var worldId = toAddWorlds[i];
                if (database.myStoredWorlds.FindById(worldId) == null)
                {
                    await TaskUtilities.YieldToMainThread();
                    new ApiWorld {id = worldId}.Fetch(); // it will get intercepted and stored
                    await Task.Delay(TimeSpan.FromSeconds(5f + Random.Range(0f, 5f))).ConfigureAwait(false);
                }
            }

            ImportStatusInner = "Creating favorites list";
            await TaskUtilities.YieldToMainThread();
            var categoryName = $"Imported from {fileName}";

            void DoAddCategories<T>(List<string> ids, DatabaseFavoriteHandler<T> favs, ExtendedFavoritesModuleBase<T>? module, ILiteCollection<T> rawStore) where T: class, INamedStoredObject
            {
                if (ids.Count == 0) return;
                
                var existingCategory = favs.GetCategory(categoryName);
                foreach (var id in ids)
                {
                    if (favs.IsFavorite(id, categoryName))
                        continue;
                
                    var storedAvatar = rawStore.FindById(id);
                    if (storedAvatar == null) continue;
                
                    favs.AddFavorite(id, categoryName);
                }

                if (existingCategory != null) return;
                
                existingCategory = new StoredCategory {CategoryName = categoryName, SortType = "!added"};
                favs.UpdateCategory(existingCategory);

                if (module == null) return;
                
                module.CreateList(existingCategory);
                module.ReorderLists();
            }
            
            DoAddCategories(toAddWorlds, database.WorldFavorites, FavCatMod.Instance.WorldsModule, database.myStoredWorlds);
            DoAddCategories(toAddUsers, database.PlayerFavorites, FavCatMod.Instance.PlayerModule, database.myStoredPlayers);
            
            MelonLogger.Msg($"Done importing {fileName}");
            File.Delete(filePath);
        }
        
        internal static Task MergeInForeignStore(string foreignStorePath)
        {
            return Task.Run(() =>
            {
                var database = FavCatMod.Database;
                if (database == null)
                {
                    MelonLogger.Msg("Database does not exist, can't merge");
                    return;
                }
                
                var fileName = Path.GetFileName(foreignStorePath);
                MelonLogger.Msg($"Started merging database with {fileName}");
                using var storeDatabase = new LiteDatabase(new ConnectionString {Filename = foreignStorePath, ReadOnly = true, Connection = ConnectionType.Direct});
            
                var storedAvatars = storeDatabase.GetCollection<StoredAvatar>("avatars");
                var storedPlayers = storeDatabase.GetCollection<StoredPlayer>("players");
                var storedWorlds = storeDatabase.GetCollection<StoredWorld>("worlds");

                ImportStatusInner = "Importing avatars";
                foreach (var storedAvatar in storedAvatars.FindAll())
                {
                    var existingStored = database.myStoredAvatars.FindById(storedAvatar.AvatarId);
                    if (existingStored == null || existingStored.UpdatedAt < storedAvatar.UpdatedAt)
                        database.myStoredAvatars.Upsert(storedAvatar);
                }
                
                ImportStatusInner = "Importing players";
                foreach (var storedPlayer in storedPlayers.FindAll())
                {
                    var existingStored = database.myStoredPlayers.FindById(storedPlayer.PlayerId);
                    if (existingStored == null)
                        database.myStoredPlayers.Upsert(storedPlayer);
                }
                
                ImportStatusInner = "Importing worlds";
                foreach (var storedWorld in storedWorlds.FindAll())
                {
                    var existingStored = database.myStoredWorlds.FindById(storedWorld.WorldId);
                    if (existingStored == null || existingStored.UpdatedAt < storedWorld.UpdatedAt)
                        database.myStoredWorlds.Upsert(storedWorld);
                }
                
                MelonLogger.Msg($"Done merging database with {fileName}");
            });
        }
    }
}