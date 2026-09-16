using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoinRushSurvivor.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Services.Clear();
        }

        public static void Register<T>(T service) where T : class
        {
            Register(typeof(T), service);
        }

        public static void Register(Type type, object service)
        {
            if (type == null)
            {
                Debug.LogError("ServiceLocator received a null type.");
                return;
            }

            if (service == null)
            {
                Debug.LogError($"ServiceLocator cannot register a null instance for {type.Name}.");
                return;
            }

            if (Services.TryGetValue(type, out var existing))
            {
                if (ReferenceEquals(existing, service))
                {
                    return;
                }

                Debug.LogWarning($"ServiceLocator is replacing an existing registration for {type.Name}.");
            }

            Services[type] = service;
        }

        public static T Get<T>() where T : class
        {
            if (TryGet<T>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var rawService) && rawService is T typedService)
            {
                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        public static void Unregister<T>(T service = null) where T : class
        {
            var type = typeof(T);

            if (!Services.TryGetValue(type, out var existing))
            {
                return;
            }

            if (service != null && !ReferenceEquals(existing, service))
            {
                return;
            }

            Services.Remove(type);
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
