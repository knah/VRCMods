using System;
using UnityEngine;

namespace UIExpansionKit.API
{
    public interface ICustomLayoutedMenu
    {
        /// <summary>
        /// Adds a simple button to this custom menu
        /// </summary>
        /// <param name="text">User-visible button text</param>
        /// <param name="onClick">Button click action</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the button is instantiated</param>
        void AddSimpleButton(string text, Action onClick, Action<GameObject> instanceConsumer = null);

        /// <summary>
        /// Adds a toggle button to this custom menu
        /// </summary>
        /// <param name="text">User-visible button text</param>
        /// <param name="onClick">This action will be called when button state is toggled</param>
        /// <param name="getInitialState">(optional) this func will be called to get the initial state of this button. If will default to not-set if this is not provided.</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the button is instantiated</param>
        void AddToggleButton(string text, Action<bool> onClick, Func<bool> getInitialState = null, Action<GameObject> instanceConsumer = null);
        
        /// <summary>
        /// Registers a custom button prefab. This prefab can be instantiated multiple times. 
        /// </summary>
        /// <param name="gameObject">Button prefab</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the prefab is instantiated</param>
        void AddCustomButton(GameObject gameObject, Action<GameObject> instanceConsumer = null);
        
        /// <summary>
        /// Adds a label to custom menu
        /// </summary>
        /// <param name="text">User-visible text</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the label is instantiated</param>
        void AddLabel(string text, Action<GameObject> instanceConsumer = null);

        /// <summary>
        /// Adds an empty spot in menu layout.
        /// </summary>
        void AddSpacer();

        /// <summary>
        /// This event is called when this menu's content root in created.
        /// Your mod code should subscribe to this event if you want to add custom gameobjects to the menu.
        /// </summary>
        event Action<GameObject> OnContentRootCreated;
        
        /// <summary>
        /// If true, created buttons will be more appropriate for quick menu usage (i.e. square buttons) as opposed to plain menu lists.
        /// </summary>
        void SetUseQuickMenuLikeComponents(bool isQuickMenu);
    }
}