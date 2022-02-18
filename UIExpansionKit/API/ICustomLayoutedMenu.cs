using System;
using System.Diagnostics.CodeAnalysis;
using UIExpansionKit.API.Controls;
using UnityEngine;

namespace UIExpansionKit.API
{
    [SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter")]
    public interface ICustomLayoutedMenu
    {
        [Obsolete("Use the overload without instanceConsumer")]
        void AddSimpleButton(string text, Action onClick, Action<GameObject> instanceConsumer = null);

        /// <summary>
        /// Adds a simple button to this custom menu
        /// </summary>
        /// <param name="text">User-visible button text</param>
        /// <param name="onClick">Button click action</param>
        IMenuButton AddSimpleButton(string text, Action onClick);
        
        /// <summary>
        /// Adds a simple button to this custom menu
        /// </summary>
        /// <param name="text">User-visible button text, receives the pressed button as parameter</param>
        /// <param name="onClick">Button click action</param>
        IMenuButton AddSimpleButton(string text, Action<IMenuButton> onClick);

        [Obsolete("Use the overload without instanceConsumer")]
        void AddToggleButton(string text, Action<bool> onClick, Func<bool> getInitialState = null, Action<GameObject> instanceConsumer = null);
        
        /// <summary>
        /// Adds a toggle button to this custom menu
        /// </summary>
        /// <param name="text">User-visible button text</param>
        /// <param name="onClick">This action will be called when button state is toggled</param>
        /// <param name="getInitialState">(optional) this func will be called to get the initial state of this button. If will default to not-set if this is not provided.</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the button is instantiated</param>
        IMenuToggle AddToggleButton(string text, Action<bool> onClick, Func<bool> getInitialState = null);
        
        [Obsolete("Use the overload without instanceConsumer")]
        void AddCustomButton(GameObject gameObject, Action<GameObject> instanceConsumer = null);
        
        /// <summary>
        /// Registers a custom button prefab. This prefab can be instantiated multiple times. 
        /// </summary>
        /// <param name="gameObject">Button prefab</param>
        IMenuControl AddCustomButton(GameObject gameObject);

        [Obsolete("Use the overload without instanceConsumer")]
        void AddLabel(string text, Action<GameObject> instanceConsumer = null);
        
        /// <summary>
        /// Adds a label to custom menu
        /// </summary>
        /// <param name="text">User-visible text</param>
        IMenuLabel AddLabel(string text);

        /// <summary>
        /// Adds an empty spot in menu layout.
        /// </summary>
        void AddSpacer();

        /// <summary>
        /// Adds an empty spot in menu layout.
        /// </summary>
        IMenuControl AddSpacerEx();

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