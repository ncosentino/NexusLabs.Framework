using System;

namespace NexusLabs.Contracts
{
    public sealed class ContractException : Exception
    {
        public ContractException(string message)
            : base(message)
        {
        }
    }
}
