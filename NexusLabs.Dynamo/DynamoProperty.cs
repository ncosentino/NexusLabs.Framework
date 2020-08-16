namespace NexusLabs.Dynamo
{
    public sealed class DynamoProperty : IDynamoProperty
    {
        public DynamoProperty(
            DynamoGetterDelegate getter,
            DynamoSetterDelegate setter)
        {
            Getter = getter;
            Setter = setter;
        }

        public DynamoProperty(DynamoGetterDelegate getter)
            : this(getter, null)
        {
        }

        public DynamoProperty(DynamoSetterDelegate setter)
            : this(null, setter)
        {
        }

        public DynamoGetterDelegate Getter { get; }

        public DynamoSetterDelegate Setter { get; }
    }
}
