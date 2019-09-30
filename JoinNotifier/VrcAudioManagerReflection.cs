using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JoinNotifier
{
    public static class VrcAudioManagerReflection
    {
        private static readonly FieldInfo ourManagerInstanceField;

        static VrcAudioManagerReflection()
        {
            ourManagerInstanceField = typeof(VRCAudioManager).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(it => it.FieldType == typeof(VRCAudioManager));
        }
        
        public static VRCAudioManager GetAudioManager()
        {
            return (VRCAudioManager) ourManagerInstanceField.GetValue(null);
        }

        public static IEnumerator WaitForAudioManager()
        {
            yield return new WaitWhile(() => GetAudioManager() == null);
        }
    }
}