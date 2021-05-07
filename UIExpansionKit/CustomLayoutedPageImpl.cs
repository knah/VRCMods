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

        internal void SortButtonByTextRespectingLabels()
        {
            if (RegisteredButtons.Count <= 1) return;
            List<(int start, int count)> labels = new();

            (int start, int count) currentLabel = new(0, 0);
            for (int i = 0; i < RegisteredButtons.Count; i++)
            {
                var currentButton = RegisteredButtons[i];
                // this way both labels and spacers should be included
                if (currentButton.Action is null && currentButton.ToggleAction is null)
                {
                    labels.Add(currentLabel);
                    currentLabel.start = i;
                    currentLabel.count = 0;
                    continue;
                }

                currentLabel.count++;
            }
            labels.Add(currentLabel);

            ButtonComparer bc = new();
            foreach ((int start, int count) in labels)
            {
                RegisteredButtons.Sort(Mathf.Min(start + 1, RegisteredButtons.Count-1), count, bc);
            }
        }

        private class ButtonComparer : IComparer<ExpansionKitApi.ButtonRegistration>
        {
            public int Compare(ExpansionKitApi.ButtonRegistration x, ExpansionKitApi.ButtonRegistration y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
            }

        }

        internal void SortButtonsByText()
        {
            RegisteredButtons.Sort((button1, button2) => string.CompareOrdinal(button1.Text, button2.Text));
        }

        public void AddSimpleButton(string text, Action onClick, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, Action = onClick, InstanceConsumer = instanceConsumer });
        }

        public void AddToggleButton(string text, Action<bool> onClick, Func<bool> getInitialState = null, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(
                new ExpansionKitApi.ButtonRegistration
                    {
                        Text = text, ToggleAction = onClick, InitialState = getInitialState, InstanceConsumer = instanceConsumer
                    });
        }

        public void AddCustomButton(GameObject gameObject, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Prefab = gameObject, InstanceConsumer = instanceConsumer });
        }

        public void AddLabel(string text, Action<GameObject> instanceConsumer = null)
        {
            RegisteredButtons.Add(new ExpansionKitApi.ButtonRegistration { Text = text, InstanceConsumer = instanceConsumer });
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