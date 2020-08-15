using System;

namespace NexusLabs.Framework
{
    public interface ICast
    {
        T ToType<T>(object obj);

        T ToType<T>(
            object obj,
            bool useCache);

        object ToType(
            object obj,
            Type resultType);
        
        object ToType(
            object obj,
            Type resultType,
            bool useCache);
    }
}