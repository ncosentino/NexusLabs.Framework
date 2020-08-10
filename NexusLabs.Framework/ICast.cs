using System;

namespace NexusLabs.Framework
{
    public interface ICast
    {
        object ToType(
            object obj,
            Type resultType);
        
        object ToType(
            object obj,
            Type resultType,
            bool useCache);
    }
}