namespace NexusLabs.Dynamo.Properties
{
    public sealed class SimpleDynamoProperty<T> : IDynamoProperty
    {
        private T _value;

        public SimpleDynamoProperty(T initialValue)
            : this()
        {
            Setter.Invoke(null, initialValue);
        }

        public SimpleDynamoProperty()
        {
            Getter = _ => _value;
            Setter = (_, v) => _value = (T)v;
        }

        public DynamoGetterDelegate Getter { get; }

        public DynamoSetterDelegate Setter { get; }
    }
}
