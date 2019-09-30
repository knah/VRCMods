using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using VRC;

namespace JoinNotifier
{
    public static class NetworkManagerHooks
    {
        private static readonly Type ourNmType;
        private static readonly FieldInfo ourNmInstanceField;
        private static readonly FieldInfo ourPlayerJoinEvent;
        
        private static object GetInstance()
        {
            return ourNmInstanceField.GetValue(null);
        }

        static NetworkManagerHooks()
        {
            ourNmType = typeof(PlayerModManager).Assembly.GetType("NetworkManager");
            ourNmInstanceField = ourNmType.GetField("Instance", BindingFlags.Static | BindingFlags.Public);
            ourPlayerJoinEvent = ourNmType.GetField("OnPlayerJoinedEvent", BindingFlags.Instance | BindingFlags.Public);
        }

        public static IEnumerator WaitForNmInit()
        {
            return new WaitWhile(() => GetInstance() == null);
        }

        public static void AddPlayerJoinHook(UnityAction<Player> action)
        {
            new UnityActionReflection<Player>(ourPlayerJoinEvent.FieldType, ourPlayerJoinEvent.GetValue(GetInstance())).Add(action);
        }
    }
}