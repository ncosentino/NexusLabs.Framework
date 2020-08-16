namespace NexusLabs.Dynamo
{
    internal interface IDynamo : IReadOnlyDynamo
    {
        bool RegisterGetter(
            string memberName,
            DynamoGetterDelegate getter);

        bool RegisterSetter(
            string memberName,
            DynamoSetterDelegate setter);
    }
}
