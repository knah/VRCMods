using System;
using MelonLoader;
using VRC;

namespace JoinNotifier
{
    public static class NetworkManagerHooks
    {
        private static bool IsInitialized;
        private static bool SeenFire;
        private static bool AFiredFirst;

        public static event Action<Player> OnJoin;
        public static event Action<Player> OnLeave;
        
        public static void EventHandlerA(Player player)
        {
            if (!SeenFire)
            {
                AFiredFirst = true;
                SeenFire = true;
                
                MelonModLogger.Log("[JoinNotifier] A fired first");
            }
            
            (AFiredFirst ? OnJoin : OnLeave)?.Invoke(player);
        }
        
        public static void EventHandlerB(Player player)
        {
            if (!SeenFire)
            {
                AFiredFirst = false;
                SeenFire = true;
                
                MelonModLogger.Log("[JoinNotifier] B fired first");
            }
            
            (AFiredFirst ? OnLeave : OnJoin)?.Invoke(player);
        }

        public static void Initialize()
        {
            if (IsInitialized) return;
            if (ReferenceEquals(NetworkManager.field_Internal_Static_NetworkManager_0, null)) return;

            var field0 = NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_ObjectPublicHaVoUnVoTUnVoVoVoVoUnique_1_Player_0;
            var field1 = NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_ObjectPublicHaVoUnVoTUnVoVoVoVoUnique_1_Player_1;

            AddDelegate(field0, EventHandlerA);
            AddDelegate(field1, EventHandlerB);

            IsInitialized = true;
        }

        private static void AddDelegate(ObjectPublicHaVoUnVoTUnVoVoVoVoUnique<Player> field, Action<Player> eventHandlerA)
        {
            field.field_Private_HashSet_1_UnityAction_1_T_0.Add(eventHandlerA);
        }
    }
}