using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UIExpansionKit.API;
using UIExpansionKit.API.Controls;
using UIExpansionKit.ControlsImpl;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit
{
    [SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter")]
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

        public IMenuButton AddSimpleButton(string text, Action onClick)
        {
            var control = new DummyControlWithText(text, TextAnchor.MiddleCenter);
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, Action = onClick, InstanceConsumer = control.ConsumeGameObject});
            return control;
        }

        public IMenuButton AddSimpleButton(string text, Action<IMenuButton> onClick)
        {
            var control = new DummyControlWithText(text, TextAnchor.MiddleCenter);
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, Action = () => onClick(control), InstanceConsumer = control.ConsumeGameObject});
            return control;
        }

        public void AddToggleButton(string text, Action<bool> onClick, Func<bool> getInitialState = null, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, ToggleAction = onClick, InitialState = getInitialState, InstanceConsumer = instanceConsumer});
        }

        public IMenuToggle AddToggleButton(string text, Action<bool> onClick, Func<bool> getInitialState = null)
        {
            var control = new MenuToggle(text, TextAnchor.MiddleCenter, getInitialState?.Invoke() ?? false);
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, ToggleAction = onClick, InitialState = getInitialState, InstanceConsumer = control.ConsumeGameObject});
            return control;
        }

        public void AddCustomButton(GameObject gameObject, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Prefab = gameObject, InstanceConsumer = instanceConsumer});
        }

        public IMenuControl AddCustomButton(GameObject gameObject)
        {
            var control = new BaseMenuControl();
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Prefab = gameObject, InstanceConsumer = control.ConsumeGameObject});
            return control;
        }

        public void AddLabel(string text, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, InstanceConsumer = instanceConsumer});
        }

        public IMenuLabel AddLabel(string text)
        {
            var control = new DummyControlWithText(text, TextAnchor.MiddleLeft);
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, InstanceConsumer = control.ConsumeGameObject});
            return control;
        }

        public void AddSpacer()
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { });
        }

        public IMenuControl AddSpacerEx()
        {
            var control = new BaseMenuControl();
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { InstanceConsumer = control.ConsumeGameObject});
            return control;
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