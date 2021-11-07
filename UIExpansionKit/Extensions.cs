using System.Collections;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.DataModel;
using VRC.DataModel.Core;
using VRCSDK2;

#nullable enable

namespace UIExpansionKit
{
    public static class Extensions
    {
        public static void DestroyChildren(this Transform parent)
        {
            for (var i = parent.childCount; i > 0; i--) 
                Object.DestroyImmediate(parent.GetChild(i - 1).gameObject);
        }

        public static GameObject NoUnload(this GameObject obj)
        {
            obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return obj;
        }

        public static T GetOrAddComponent<T>(this GameObject obj) where T: Component
        {
            var result = obj.GetComponent<T>();
            if (result == null) 
                result  = obj.AddComponent<T>();
            return result;
        }

        internal static void AddUiShapeWithTriggerCollider(this GameObject obj)
        {
            obj.AddComponent<VRC_UiShape>().SetupCollision();
            MelonCoroutines.Start(TriggerifyBoxColliderWhenItAppears(obj));
        }

        private static IEnumerator TriggerifyBoxColliderWhenItAppears(GameObject gameObject)
        {
            while (gameObject)
            {
                var collider = gameObject.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                    yield break;
                }
                yield return null;
            }
        }

        public static Player? GetPlayer(this IUser user)
        {
            var trueUser = user.TryCast<DataModel<APIUser>>();
            if (trueUser == null) return null;

            return trueUser.field_Protected_TYPE_0.GetPlayer();
        }
        
        public static Player? GetPlayer(this APIUser user)
        {
            foreach (var pl in PlayerManager.prop_PlayerManager_0.field_Private_List_1_Player_0)
            {
                if (pl.field_Private_APIUser_0?.id != user.id) continue;

                return pl;
            }

            return null;
        }
    }
}