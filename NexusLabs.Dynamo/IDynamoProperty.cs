namespace NexusLabs.Dynamo
{
    public interface IDynamoProperty
    {
        DynamoGetterDelegate Getter { get; }
        DynamoSetterDelegate Setter { get; }
    }
}