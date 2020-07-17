using Il2CppSystem.Collections.Generic;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.Animations;

namespace AdvancedSafety
{
    public static class ComponentAdjustment
    {
        public static void VisitAudioSource(this AudioSource audioSource, ref int totalCount, ref int deletedCount, ref int specificCount, GameObject obj, System.Collections.Generic.List<AudioSource> sourcesOutList)
        {
            totalCount++;
            
            if (specificCount++ >= AdvancedSafetySettings.MaxAudioSources)
            {
                Object.DestroyImmediate(audioSource, true);
                deletedCount++;

                return;
            }

            if (!AdvancedSafetySettings.AllowSpawnSounds)
            {
                sourcesOutList.Add(audioSource);
                if (audioSource.enabled && obj.activeSelf && audioSource.playOnAwake)
                {
                    audioSource.playOnAwake = false;
                    audioSource.Stop();
                }
            }

            if (!AdvancedSafetySettings.AllowGlobalSounds)
            {
                var spatializer = obj.GetComponent<ONSPAudioSource>();
                if (spatializer != null)
                {
                    spatializer.enabled = true;
                    spatializer.far = Mathf.Min(spatializer.far, 10f);
                    spatializer.volumetricRadius = Mathf.Min(spatializer.volumetricRadius, 10f);
                    spatializer.near = Mathf.Min(spatializer.near, 1f);
                    spatializer.enableSpatialization = true;
                    spatializer.gain = Mathf.Min(spatializer.gain, 1f);
                }
                
                audioSource.spatialize = true;
                audioSource.volume = Mathf.Max(audioSource.volume, 1f);
                audioSource.maxDistance = Mathf.Max(audioSource.maxDistance, 10f);
                audioSource.minDistance = Mathf.Max(audioSource.minDistance, 1f);
                audioSource.spatialBlend = 1f;
            }
        }

        public static void VisitConstraint(this IConstraint constraint, ref int totalCount, ref int deletedCount, ref int specificCount, GameObject obj)
        {
            totalCount++;

            if (specificCount++ > AdvancedSafetySettings.MaxConstraints)
            {
                Object.DestroyImmediate(constraint.Cast<Behaviour>(), true);
                deletedCount++;
            }
        }

        public static void VisitCollider(this Collider collider, ref int totalCount, ref int deletedCount, ref int specificCount, GameObject obj)
        {
            totalCount++;

            if (specificCount++ >= AdvancedSafetySettings.MaxColliders)
            {
                deletedCount++;
                var rigidbody = obj.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    deletedCount++;
                    Object.DestroyImmediate(rigidbody, true);
                }

                Object.DestroyImmediate(collider, true);
            }
        }
        
        public static void VisitGeneric(this Component rigidbody, ref int totalCount, ref int deletedCount, ref int specificCount, int maxComponents)
        {
            totalCount++;

            if (specificCount++ >= maxComponents)
            {
                deletedCount++;
                Object.DestroyImmediate(rigidbody, true);
            }
        }

        public static void VisitCloth(this Cloth cloth, ref int totalCount, ref int deletedCount, ref int specificCount, GameObject obj)
        {
            totalCount++;

            var numVertices = 0;
            var skinnedMesh = obj.GetComponent<SkinnedMeshRenderer>()?.sharedMesh;
            if (skinnedMesh == null || (specificCount += (numVertices = skinnedMesh.vertexCount)) >= AdvancedSafetySettings.MaxClothVertices)
            {
                specificCount -= numVertices;
                deletedCount++;
                Object.DestroyImmediate(cloth, true);
            }
        }
        
        private static readonly List<Material> ourMaterialsList = new List<Material>();
        
        public static void VisitRenderer(this Renderer renderer, ref int totalCount, ref int deletedCount, ref int polyCount, ref int materialCount, GameObject obj)
        {
            totalCount++;
            
            var skinnedMeshRenderer = renderer.TryCast<SkinnedMeshRenderer>();
            var meshFilter = obj.GetComponent<MeshFilter>();

            renderer.GetSharedMaterials(ourMaterialsList);
            if (ourMaterialsList.Count == 0) return;
            
            var mesh = skinnedMeshRenderer?.sharedMesh ?? meshFilter?.sharedMesh;
            if (mesh != null)
            {
                if (polyCount + mesh.vertexCount >= AdvancedSafetySettings.MaxPolygons)
                {
                    renderer.SetMaterialArray(new Il2CppReferenceArray<Material>(0));

                    deletedCount++;
                    
                    return;
                }

                polyCount += mesh.vertexCount;

                if (AdvancedSafetySettings.HeuristicallyRemoveScreenSpaceBullshit && meshFilter != null && (ourMaterialsList[0]?.renderQueue ?? 0) >= 2500)
                {
                    var meshLowerName = mesh.name.ToLower();
                    if (meshLowerName.Contains("sphere") || meshLowerName.Contains("cube"))
                    {
                        deletedCount++;

                        renderer.SetMaterialArray(new Il2CppReferenceArray<Material>(0));

                        return;
                    }
                }
            }

            var allowedMaterialCount = AdvancedSafetySettings.MaxMaterialSlots - materialCount;
            if (allowedMaterialCount < renderer.GetMaterialCount())
            {
                renderer.GetSharedMaterials(ourMaterialsList);
                
                deletedCount += ourMaterialsList.Count - allowedMaterialCount;

                ourMaterialsList.RemoveRange(allowedMaterialCount, ourMaterialsList.Count - allowedMaterialCount);
                renderer.materials = (Il2CppReferenceArray<Material>) ourMaterialsList.ToArray();
            }

            materialCount += renderer.GetMaterialCount();
        }
    }
}