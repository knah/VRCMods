using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using VRC;

namespace JoinNotifier
{
    public static class NetworkManagerHooks
    {
        private static readonly FieldInfo ourNmInstanceField;
        private static readonly FieldInfo ourPlayerJoinEvent;
        private static readonly FieldInfo ourPlayerLeftEvent;
        
        private static object GetInstance()
        {
            return ourNmInstanceField.GetValue(null);
        }

        static NetworkManagerHooks()
        {
            var nmType = typeof(PlayerModManager).Assembly.GetType("NetworkManager");
            ourNmInstanceField = nmType.GetField("Instance", BindingFlags.Static | BindingFlags.Public);
            ourPlayerJoinEvent = nmType.GetField("OnPlayerJoinedEvent", BindingFlags.Instance | BindingFlags.Public);
            ourPlayerLeftEvent = nmType.GetField("OnPlayerLeftEvent", BindingFlags.Instance | BindingFlags.Public);
        }

        public static IEnumerator WaitForNmInit()
        {
            return new WaitWhile(() => GetInstance() == null);
        }

        public static void AddPlayerJoinHook(UnityAction<Player> action)
        {
            new UnityActionReflection<Player>(ourPlayerJoinEvent.FieldType, ourPlayerJoinEvent.GetValue(GetInstance())).Add(action);
        }
        
        public static void AddPlayerLeftHook(UnityAction<Player> action)
        {
            new UnityActionReflection<Player>(ourPlayerLeftEvent.FieldType, ourPlayerLeftEvent.GetValue(GetInstance())).Add(action);
        }
    }
}