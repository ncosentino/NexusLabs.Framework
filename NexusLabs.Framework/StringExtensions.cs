using System.IO;
using System.Text;

namespace System;

public static class StringExtensions
{
    public static Stream ToStream(this string str, Encoding encoding)
    {
        MemoryStream stream = new(encoding.GetBytes(str))
        {
            Position = 0
        };

        return stream;
    }
}
