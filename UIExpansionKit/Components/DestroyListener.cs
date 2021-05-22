using System;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

#nullable enable

namespace UIExpansionKit.Components
{
    public class DestroyListener : MonoBehaviour
    {

        [method:HideFromIl2Cpp]
        public event Action? OnDestroyed;

        public DestroyListener(IntPtr obj0) : base(obj0)
        {
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}