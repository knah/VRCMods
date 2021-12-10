using System;

namespace LagFreeScreenshots
{
    public static class EventHandler
    {
        /// <summary>
        /// Calls when an screenshot is saved
        /// </summary>
        public static event Action<string, int, int, Metadata> OnScreenshotSaved;

        internal static void InvokeScreenshotSaved(string filePath, int width, int height, Metadata metadata) => OnScreenshotSaved?.Invoke(filePath, width, height, metadata);
    }
}
