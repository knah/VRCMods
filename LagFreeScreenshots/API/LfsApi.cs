#nullable enable

namespace LagFreeScreenshots.API
{
    public static class LfsApi
    {
        /// <summary>
        /// Called after a creenshot is taken and written to disk
        /// </summary>
        public static event ScreenshotSavedEventV2? OnScreenshotSavedV2;

        public delegate void ScreenshotSavedEventV2(string filePath, int width, int height, MetadataV2? metadata);

        internal static void InvokeScreenshotSaved(string filePath, int width, int height, MetadataV2? metadataV2) =>
            OnScreenshotSavedV2?.Invoke(filePath, width, height, metadataV2);
    }
}