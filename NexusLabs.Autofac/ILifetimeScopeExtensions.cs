using System;

namespace Autofac
{
    public static class ILifetimeScopeExtensionMethods
    {
        public static bool TryResolveAndSet<T>(
            this ILifetimeScope scope,
            ref T instance)
            where T : class
        {
            if (instance != default(T))
            {
                return true;
            }

            if (instance == null)
            {
                instance = scope?.Resolve<T>();
            }

            return instance != null;
        }
    }
}
