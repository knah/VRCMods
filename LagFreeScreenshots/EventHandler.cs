using System;
using LagFreeScreenshots.API;

namespace LagFreeScreenshots
{
    [Obsolete("Use LagFreeScreenshots.API.LfsApi")]
    public static class EventHandler
    {
        /// <summary>
        /// Calls when an screenshot is saved
        /// </summary>
        public static event Action<string, int, int, Metadata> OnScreenshotSaved;

        internal static void InvokeScreenshotSaved(string filePath, int width, int height, MetadataV2 metadata)
        {
            OnScreenshotSaved?.Invoke(filePath, width, height, metadata == null ? null : new Metadata(metadata));
        }
    }
}
