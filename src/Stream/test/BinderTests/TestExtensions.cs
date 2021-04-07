using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Stream.Binder
{
    public static class TestExtensions
    {
        public static byte[] GetBytes(this string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }

        public static string GetString(this byte[] input)
        {
            return Encoding.UTF8.GetString(input);
        }
    }
}
