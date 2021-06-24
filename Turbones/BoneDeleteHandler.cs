using System;
using UnityEngine;

namespace Turbones
{
    public class BoneDeleteHandler : MonoBehaviour
    {
        public BoneDeleteHandler(IntPtr ptr) : base(ptr)
        {
        }

        private void OnDestroy()
        {
            foreach (var bone in GetComponents<DynamicBone>())
            {
                JigglySolverApi.DynamicBoneOnDestroyPatch(bone.Pointer);
            }
        }
    }
}