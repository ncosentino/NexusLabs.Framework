using System;
using System.ComponentModel;

namespace NexusLabs.Dynamo.Properties
{
    public delegate bool CheckChangedCallback<T>(T newValue, T oldValue);

    public sealed class NotifyChangedDynamoProperty<T> : IDynamoProperty
    {
        private readonly CheckChangedCallback<T> _checkChangedCallback;
        private T _value;

        public NotifyChangedDynamoProperty()
            : this(null)
        {
        }

        public NotifyChangedDynamoProperty(CheckChangedCallback<T> checkChangedCallback)
        {
            if (checkChangedCallback == null)
            {
                checkChangedCallback = (n, o) =>
                {
                    return !Equals(n, o);
                };
            }

            _checkChangedCallback = checkChangedCallback;
            Getter = _ => _value;
            Setter = (propertyName, v) =>
            {
                if (!_checkChangedCallback.Invoke((T)v, _value))
                {
                    return;
                }

                _value = (T)v;
                Changed?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            };
        }

        public event EventHandler<PropertyChangedEventArgs> Changed;

        public DynamoGetterDelegate Getter { get; }

        public DynamoSetterDelegate Setter { get; }
    }
}
