using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FavCat.Database;
using FavCat.Database.Stored;

namespace FavCat
{
    public static class ExportProcessor
    {
        public static bool IsExportingFavorites;
        public static int TotalCategories;
        public static int ProcessedCategories;
        public static async Task DoExportFavorites<T>(DatabaseFavoriteHandler<T> favorites) where T:class, INamedStoredObject
        {
            IsExportingFavorites = true;
            ProcessedCategories = 0;
            TotalCategories = 1;
            try
            {
                await Task.Delay(100);

                var exportDir = "UserData/FavCatExport";
                if (!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);

                var storedCategories = favorites.GetCategories().ToList();
                TotalCategories = storedCategories.Count;
                foreach (var category in storedCategories)
                {
                    var fileName = favorites.EntityType + "-" + SanitizeForFileName(category.CategoryName) + ".txt";
                    using var writer = new StreamWriter(exportDir + "/" + fileName);
                    foreach (var listFavorite in favorites.ListFavorites(category.CategoryName).ToList())
                    {
                        var authorNameSuffix = listFavorite.Item2.AuthorName;
                        authorNameSuffix = authorNameSuffix == null ? "" : $" by {authorNameSuffix}";
                        await writer.WriteLineAsync(listFavorite.Item1.ObjectId + " " + listFavorite.Item2.Name + authorNameSuffix)
                            .ConfigureAwait(false);
                    }

                    ProcessedCategories++;
                }
            }
            finally
            {
                IsExportingFavorites = false;
            }
        }

        private static readonly Regex ourBadFileChars = new Regex("[\\/+:*?<>\"|]"); 
        private static string SanitizeForFileName(string s)
        {
            return ourBadFileChars.Replace(s, "");
        }
    }
}