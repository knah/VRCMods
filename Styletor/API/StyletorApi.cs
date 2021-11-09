using System;
using System.Collections.Generic;
using System.IO;

namespace Styletor.API
{
    /// <summary>
    /// Provides an API for other mods to be used
    /// Be aware that Styletor is GPLv3 (unlike UI Expansion Kit, which is LGPLv3), so your mod has to be provided under a GPL-compatible license (so [a]GPLv3) to use these in any form
    /// Check README for another way to provide styles embedded in a mod without using these
    /// </summary>
    public static class StyletorApi
    {
        internal static readonly List<Func<IEnumerable<KeyValuePair<string, Stream>>>> StyleProviders = new();

        /// <summary>
        /// Registers the given style provider.
        /// The provided Func should return an enumerable with named ZIP file streams with styles
        /// Streams returned will be closed by Styletor
        /// This Func can be called at any time when styles are being reloaded
        /// </summary>
        public static void RegisterStylesProvider(Func<IEnumerable<KeyValuePair<string, Stream>>> zipStyleStreams)
        {
            StyleProviders.Add(zipStyleStreams);
            ReloadStyles();
        }

        /// <summary>
        /// Reloads all styles, including styles on disk and style providers
        /// </summary>
        public static void ReloadStyles()
        {
            StyletorMod.Instance.ReloadStyles();
        }
    }
}