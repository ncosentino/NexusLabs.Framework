using System;
using System.Collections.Generic;
using System.Dynamic;

namespace NexusLabs.Dynamo
{
    internal sealed class Dynamo :
        DynamicObject,
        IDynamo
    {
        private readonly Dictionary<string, object> _cachedMemberMapping;
        private readonly List<TryGetDynamoMemberDelegate> _tryGetMemberCallbacks;

        public Dynamo(IEnumerable<KeyValuePair<string, object>> members)
            : this()
        {
            foreach (var member in members)
            {
                if (!TrySetMember(member.Key, member.Value))
                {
                    throw new ArgumentException(
                        $"Could not set member '{member.Key}'.");
                }
            }
        }

        public Dynamo(IEnumerable<TryGetDynamoMemberDelegate> tryGetMemberCallbacks)
            : this()
        {
            _tryGetMemberCallbacks.AddRange(tryGetMemberCallbacks);
        }

        public Dynamo()
        {
            _cachedMemberMapping = new Dictionary<string, object>();
            _tryGetMemberCallbacks = new List<TryGetDynamoMemberDelegate>();
        }

        public bool TrySetMember(
            string memberName,
            object value)
        {
            _cachedMemberMapping[memberName] = value;
            return true;
        }

        public override bool TrySetMember(
            SetMemberBinder binder,
            object value) => TrySetMember(
                binder.Name, value);

        public override bool TryGetMember(
            GetMemberBinder binder,
            out object result)
        {
            if (_cachedMemberMapping.TryGetValue(
                binder.Name,
                out result))
            {
                return true;
            }

            if (!base.TryGetMember(
                binder,
                out result))
            {
                foreach (var callback in _tryGetMemberCallbacks)
                {
                    if (callback.Invoke(
                        binder,
                        out result))
                    {
                        TrySetMember(
                            binder.Name,
                            result);
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        public void RegisterCallback(TryGetDynamoMemberDelegate callback)
        {
            _tryGetMemberCallbacks.Add(callback);
        }
    }
}
