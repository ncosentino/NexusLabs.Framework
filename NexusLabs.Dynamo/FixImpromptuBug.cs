using System;
using System.Collections.Generic;
using System.Linq;

using ImpromptuInterface.Build;
using ImpromptuInterface.Optimization;

namespace NexusLabs.Dynamo
{
    internal static class FixImpromptuBug
    {
        internal static object InitializeProxy(Type proxytype, object original, IEnumerable<Type> interfaces = null, IDictionary<string, Type> propertySpec = null)
        {
            var tProxy = (IActLikeProxyInitialize)Activator.CreateInstance(proxytype);
            tProxy.Initialize(original, interfaces, propertySpec);
            return tProxy;
        }

        public static object ActLike(this object originalDynamic, Type type, params Type[] otherInterfaces)
        {
            originalDynamic = originalDynamic.GetTargetContext(
                out var tContext,
                out _);
            tContext = tContext.FixContext();

            var tProxy = BuildProxy.BuildType(tContext, type, otherInterfaces);
            return InitializeProxy(
                tProxy,
                originalDynamic,
                new[] { type }.Concat(otherInterfaces));
        }
    }
}
