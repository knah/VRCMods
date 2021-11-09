using System.Collections.Generic;

namespace Styletor.Jsons
{
    public class StyleMetadata
    {
        /// <summary>
        /// User-visible name of this style
        /// </summary>
        public string Name = UnnamedName;

        /// <summary>
        /// User-visible description of this style
        /// </summary>
        public string Description = "";

        /// <summary>
        /// User-visible author name of this style
        /// </summary>
        public string Author = "";
        
        /// <summary>
        /// VRC build number this style was created for, i.e. "1137"
        /// </summary>
        public string? VrcBuildNumber;

        /// <summary>
        /// If true, this style is a mix-in and can be disabled or enabled (as opposed to chosen). It will be applied after VRC base style
        /// </summary>
        public bool IsMixin;
        
        /// <summary>
        /// Sorting priority of this mix-in relative to others.
        /// Mixins with larger numbers will override properties from mixins with lower numbers.
        /// User-chosen non-mixin style is applied at priority 0
        /// </summary>
        public int MixinPriority = 1;

        /// <summary>
        /// If true, this mix-in starts disabled by default.
        /// Mostly useful for bundled styles.
        /// </summary>
        public bool DisabledByDefault;

        /// <summary>
        /// A list of image names that will be turned into grayscale and overridden.
        /// VRChat has some graphics with baked-in colors, which usually makes them non-colorable.
        /// Use this so that you don't have to include (copyrighted) VRChat assets turned grayscale into your skin.
        /// </summary>
        public List<string> SpritesToGrayscale = new();

        public const string UnnamedName = "<unnamed>";
    }
}