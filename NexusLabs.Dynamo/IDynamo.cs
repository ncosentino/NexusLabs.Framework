namespace NexusLabs.Dynamo
{
    internal interface IDynamo : IReadOnlyDynamo
    {
        bool TrySetMember(
            string memberName,
            object value);
    }
}
