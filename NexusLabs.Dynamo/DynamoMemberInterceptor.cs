using System;
using System.Collections.Generic;
using System.Linq;

using Castle.DynamicProxy;

namespace NexusLabs.Dynamo
{
    [Serializable]
    public sealed class DynamoMemberInterceptor : IInterceptor
    {
        private readonly Dictionary<string, DynamoGetterDelegate> _getMemberMapping;
        private readonly Dictionary<string, DynamoSetterDelegate> _setMemberMapping;

        public DynamoMemberInterceptor(
            Type type,
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters)
        {
            _getMemberMapping = new Dictionary<string, DynamoGetterDelegate>();
            _setMemberMapping = new Dictionary<string, DynamoSetterDelegate>();

            foreach (var member in getters)
            {
                if (!HasVirtualMember(type, member.Key))
                {
                    throw new ArgumentException(
                        $"Could not set member '{member.Key}' for type " +
                        $"'{type}' because it must be marked as virtual.");
                }

                if (!RegisterGetter(member.Key, member.Value))
                {
                    throw new ArgumentException(
                        $"Could not set member '{member.Key}' for type " +
                        $"'{type}'");
                }
            }

            foreach (var member in setters)
            {
                if (!HasVirtualMember(type, member.Key))
                {
                    throw new ArgumentException(
                        $"Could not set member '{member.Key}' for type " +
                        $"'{type}' because it must be marked as virtual.");
                }

                if (!RegisterSetter(member.Key, member.Value))
                {
                    throw new ArgumentException(
                        $"Could not set member '{member.Key}' for type " +
                        $"'{type}'");
                }
            }
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name.StartsWith("set_", StringComparison.Ordinal) &&
                invocation.Method.ReturnType == typeof(void) &&
                invocation.Arguments.Length == 1)
            {
                var setterName = invocation.Method.Name.Substring("set_".Length);
                if (_setMemberMapping.TryGetValue(
                    setterName,
                    out var propertySetter))
                {
                    propertySetter.Invoke(
                        setterName,
                        invocation.Arguments.First());
                    invocation.Proceed();
                    return;
                }
            }
            else if (invocation.Method.Name.StartsWith("get_", StringComparison.Ordinal) &&
                invocation.Method.ReturnType != typeof(void) &&
                invocation.Arguments.Length == 0)
            {
                var getterName = invocation.Method.Name.Substring("get_".Length);
                if (_getMemberMapping.TryGetValue(
                    getterName,
                    out var propertyGetter))
                {
                    invocation.ReturnValue = propertyGetter.Invoke(getterName);
                    invocation.Proceed();
                    return;
                }
            }
            else if (_getMemberMapping.TryGetValue(
                invocation.Method.Name,
                out var method))
            {
                invocation.ReturnValue = method.Invoke(invocation.Method.Name);
                invocation.Proceed();
                return;
            }

            invocation.Proceed();
        }

        private bool HasVirtualMember(Type type, string memberName) =>
            type.GetProperty(memberName)?.GetMethod?.IsVirtual == true ||
            type.GetProperty(memberName)?.SetMethod?.IsVirtual == true ||
            type.GetMethod(memberName)?.IsVirtual == true;

        private bool RegisterGetter(
            string memberName,
            DynamoGetterDelegate getter)
        {
            _getMemberMapping[memberName] = getter;
            return true;
        }

        private bool RegisterSetter(
            string memberName,
            DynamoSetterDelegate setter)
        {
            _setMemberMapping[memberName] = setter;
            return true;
        }
    }
}
