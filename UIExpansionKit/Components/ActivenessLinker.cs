using System;
using UnityEngine;

namespace UIExpansionKit.Components
{
    public class ActivenessLinker : MonoBehaviour
    {
        public GameObject LinkedObject;
        
        public ActivenessLinker(IntPtr obj0) : base(obj0)
        {
        }

        private void OnEnable()
        {
            LinkedObject.SetActive(true);
        }

        private void OnDisable()
        {
            LinkedObject.SetActive(true);
        }
    }
}