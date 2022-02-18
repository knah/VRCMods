using System;
using UnityEngine;

#nullable enable

namespace UIExpansionKit.Components
{
    public class StyleElementWrapper : MonoBehaviour
    {
        public StyleElementWrapper(IntPtr obj0) : base(obj0)
        {
        }

        public string? AdditionalClass;

        private void Awake()
        {
            StylingHelper.ApplyStyling(this);
        }
    }
}