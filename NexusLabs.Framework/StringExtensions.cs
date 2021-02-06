using System.IO;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static Stream ToStream(this string str, Encoding encoding)
        {
            var stream = new MemoryStream(encoding.GetBytes(str));
            stream.Position = 0;
            return stream;
        }
    }
}
