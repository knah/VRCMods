using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Events;

namespace JoinNotifier
{
    public struct UnityActionReflection<T>
    {
        // ReSharper disable once StaticMemberInGenericType - intended behaviour
        private static FieldInfo ourInternalHashSetField;
        [NotNull] private readonly object myInstance;

        public UnityActionReflection(Type type, [NotNull] object instance)
        {
            myInstance = instance;

            InitReflectionFields(type);
        }

        private static void InitReflectionFields(Type type)
        {
            if (ourInternalHashSetField != null)
                return;

            ourInternalHashSetField = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        }
        
        public void Add(UnityAction<T> action)
        {
            ((HashSet<UnityAction<T>>) ourInternalHashSetField.GetValue(myInstance)).Add(action);
        }
    }
}
