using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace UIExpansionKit.API
{
    public static class ExpansionKitApi
    {
        internal static readonly Dictionary<ExpandedMenu, List<ButtonRegistration>> RegisteredButtons = new Dictionary<ExpandedMenu, List<ButtonRegistration>>();
        internal static readonly Dictionary<string, GameObject> CustomCategoryUIs = new Dictionary<string, GameObject>();
        
        /// <summary>
        /// Register a simple button for given menu
        /// </summary>
        /// <param name="menu">Menu to attach this button to</param>
        /// <param name="text">User-visible button text</param>
        /// <param name="onClick">Button click action</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the button is instantiated</param>
        public static void RegisterSimpleMenuButton(ExpandedMenu menu, string text, Action onClick, Action<GameObject> instanceConsumer = null)
        {
            if(!RegisteredButtons.ContainsKey(menu))
                RegisteredButtons[menu] = new List<ButtonRegistration>();

            RegisteredButtons[menu].Add(new ButtonRegistration { Text = text, Action = onClick, InstanceConsumer = instanceConsumer});
        }

        /// <summary>
        /// Registers a custom button prefab. This prefab can be instantiated multiple times. 
        /// </summary>
        /// <param name="menu">Menu to attach this button to</param>
        /// <param name="gameObject">Button prefab</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the prefab is instantiated</param>
        public static void RegisterCustomMenuButton(ExpandedMenu menu, GameObject gameObject, Action<GameObject> instanceConsumer = null)
        {
            if(!RegisteredButtons.ContainsKey(menu))
                RegisteredButtons[menu] = new List<ButtonRegistration>();

            RegisteredButtons[menu].Add(new ButtonRegistration { Prefab = gameObject, InstanceConsumer = instanceConsumer});
        }

        /// <summary>
        /// Sets the specified GameObject as a prefab for given settings category. The prefab will be instantiated each time "Undo changes" is pressed, in addition to instantiating it on menu creation.
        /// </summary>
        /// <param name="categoryName">String passed as first parameter of <see cref="ModPrefs.RegisterCategory"/></param>
        /// <param name="categoryUi">Prefab that acts as category settings UI</param>
        public static void RegisterCustomSettingsCategory(string categoryName, GameObject categoryUi)
        {
            if (CustomCategoryUIs.ContainsKey(categoryName))
            {
                MelonModLogger.LogError($"Custom UI for category {categoryName} is already registered");
                return;
            }

            CustomCategoryUIs[categoryName] = categoryUi;
        }

        internal class ButtonRegistration
        {
            public GameObject Prefab;
            
            public Action<GameObject> InstanceConsumer;
            
            public string Text;
            public Action Action;
        }
    }
}