using System.ComponentModel;

namespace LagFreeScreenshots
{
    internal enum PresetScreenshotSizes
    {
        Default,
        Custom,
            
        [Description("Thumbnail 100x100")]
        Thumbnail,
            
        [Description("Square 1024x1024")]
        Square,
            
        [Description("1280x720")]
        _720p,
            
        [Description("1920x1080 (VRC default)")]
        _1080p,
            
        [Description("4K (3840x2160)")]
        _4K,
            
        [Description("8K (7680x4320)")]
        _8K,
            
        [Description("12K (11520x6480)")]
        _12K,
            
        [Description("16K (15360x8640)")]
        _16K,
    }
}