using System;
using System.ComponentModel;

namespace NexusLabs.Dynamo.Properties
{
    public interface INotifyChangedDynamoProperty : IDynamoProperty
    {
        event EventHandler<PropertyChangedEventArgs> Changed;
    }
}
