using System.Linq;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;

namespace AdvancedSafety
{
    /**
     * Source: FinalIkSanity (https://github.com/FenrixTheFox/FinalIKSanity, GPLv3) and Requi (contributed personally by him)
     */
    public static class FinalIkPatches
    {
        public static void ApplyPatches(HarmonyLib.Harmony harmony)
        {
            harmony.Patch(typeof(IKSolverHeuristic).GetMethods().First(m => m.Name.Equals("IsValid") && m.GetParameters().Length == 1),
                new HarmonyMethod(typeof(FinalIkPatches), nameof(IkSolverIsValid)));

            harmony.Patch(typeof(IKSolverAim).GetMethod(nameof(IKSolverAim.GetClampedIKPosition)),
                new HarmonyMethod(typeof(FinalIkPatches), nameof(IkSolverAimGetClampedIKPosition)));

            harmony.Patch(typeof(IKSolverFullBody).GetMethod(nameof(IKSolverFullBody.Solve)),
                new HarmonyMethod(typeof(FinalIkPatches), nameof(IKSolverFullBodySolve)));
            
            harmony.Patch(typeof(IKSolverFABRIKRoot).GetMethod(nameof(IKSolverFABRIKRoot.OnUpdate)),
                new HarmonyMethod(typeof(FinalIkPatches), nameof(IKSolverFABRIKRootOnUpdate)));
        }

        private static void IkSolverAimGetClampedIKPosition(ref IKSolverAim __instance)
        {
            if (__instance == null) return;
            __instance.clampSmoothing = Mathf.Clamp(__instance.clampSmoothing, 0, 2);
        }

        private static bool IkSolverIsValid(ref IKSolverHeuristic __instance, ref bool __result, ref string message)
        {
            if (__instance == null) return true;
            if (__instance.maxIterations <= 64) return true;

            __result = false;
            message = "The solver requested too many iterations.";

            return false;
        }

        private static void IKSolverFullBodySolve(ref IKSolverFullBody __instance)
        {
            if (__instance == null) return;
            __instance.iterations = Mathf.Clamp(__instance.iterations, 0, 10);
        }

        private static bool IKSolverFABRIKRootOnUpdate(ref IKSolverFABRIKRoot __instance)
        {
            if (__instance == null)
                return true;
            return __instance.iterations <= 10;
        }
    }
}