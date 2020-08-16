using System;
using System.Collections.Generic;
using System.Dynamic;

namespace NexusLabs.Dynamo
{

    internal sealed class Dynamo :
        DynamicObject,
        IDynamo
    {
        private readonly Dictionary<string, DynamoGetterDelegate> _getMemberMapping;
        private readonly Dictionary<string, DynamoSetterDelegate> _setMemberMapping;

        public Dynamo(
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters)
            : this()
        {
            foreach (var member in getters)
            {
                if (!RegisterGetter(member.Key, member.Value))
                {
                    throw new ArgumentException(
                        $"Could not set member '{member.Key}'.");
                }
            }

            foreach (var member in setters)
            {
                if (!RegisterSetter(member.Key, member.Value))
                {
                    throw new ArgumentException(
                        $"Could not set member '{member.Key}'.");
                }
            }
        }

        public Dynamo()
        {
            _getMemberMapping = new Dictionary<string, DynamoGetterDelegate>();
            _setMemberMapping = new Dictionary<string, DynamoSetterDelegate>();
        }

        public bool RegisterGetter(
            string memberName,
            DynamoGetterDelegate getter)
        {
            _getMemberMapping[memberName] = getter;
            return true;
        }

        public bool RegisterSetter(
            string memberName,
            DynamoSetterDelegate setter)
        {
            _setMemberMapping[memberName] = setter;
            return true;
        }

        public override bool TrySetMember(
            SetMemberBinder binder,
            object value)
        {
            if (_setMemberMapping.TryGetValue(
                binder.Name,
                out var setter))
            {
                setter.Invoke(
                    binder.Name,
                    value);
                return true;
            }

            return base.TrySetMember(
                binder,
                value);
        }

        public override bool TryGetMember(
            GetMemberBinder binder,
            out object result)
        {
            if (_getMemberMapping.TryGetValue(
                binder.Name,
                out var getter))
            {
                result = getter.Invoke(binder.Name);
                return true;
            }

            return base.TryGetMember(
                binder,
                out result);
        }
    }
}
