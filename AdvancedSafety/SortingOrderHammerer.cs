using System;
using UnityEngine;

namespace AdvancedSafety
{
    public class SortingOrderHammerer : MonoBehaviour
    {
        private Renderer[] myRenderersToHammer;
        
        public SortingOrderHammerer(IntPtr ptr) : base(ptr)
        {
        }

        private void Start()
        {
            myRenderersToHammer = gameObject.GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            for (var i = 0; i < myRenderersToHammer.Length; i++)
            {
                if(ReferenceEquals(myRenderersToHammer[i], null)) continue;
                
                try
                {
                    myRenderersToHammer[i].sortingOrder = 0;
                }
                catch (NullReferenceException) // this would imply a deleted renderer
                {
                    myRenderersToHammer[i] = null;
                }
            }
        }
    }
}