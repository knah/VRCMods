using System;
using System.Collections.Generic;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit
{
    internal class CustomLayoutedPageImpl : ICustomLayoutedMenu
    {
        internal readonly List<ExpansionKitApi.ButtonRegistration> RegisteredButtons = new List<ExpansionKitApi.ButtonRegistration>();
        
        internal readonly LayoutDescription? LayoutDescription;
        internal bool IsQuickMenu;
        
        public CustomLayoutedPageImpl(LayoutDescription? layoutDescription)
        {
            LayoutDescription = layoutDescription;
        }

        public void AddSimpleButton(string text, Action onClick, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, Action = onClick, InstanceConsumer = instanceConsumer});
        }

        public void AddToggleButton(string text, Action<bool> onClick, Func<bool> getInitialState = null, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, ToggleAction = onClick, InitialState = getInitialState, InstanceConsumer = instanceConsumer});
        }

        public void AddCustomButton(GameObject gameObject, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Prefab = gameObject, InstanceConsumer = instanceConsumer});
        }

        public void AddLabel(string text, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, InstanceConsumer = instanceConsumer});
        }

        public void AddSpacer()
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { });
        }

        public event Action<GameObject> OnContentRootCreated;

        public void SetUseQuickMenuLikeComponents(bool isQuickMenu)
        {
            IsQuickMenu = isQuickMenu;
        }

        internal void PopulateButtons(Transform contentRoot, bool isQuickMenu, bool adjustLayout)
        {
            if (adjustLayout)
            {
                var grid = contentRoot.GetComponent<GridLayoutGroup>();
                if (LayoutDescription != null)
                {
                    var layout = LayoutDescription.Value;
                    grid.cellSize = new Vector2(380f / layout.NumColumns, layout.RowHeight);
                    grid.constraintCount = layout.NumColumns;
                }
                else
                {
                    grid.enabled = false;
                }
            }

            foreach (var buttonRegistration in RegisteredButtons)
            {
                ButtonFactory.CreateButtonForRegistration(buttonRegistration, contentRoot, isQuickMenu);
            }

            OnContentRootCreated?.Invoke(contentRoot.gameObject);
        }
    }
}