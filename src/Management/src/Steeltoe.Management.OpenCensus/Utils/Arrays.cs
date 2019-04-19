using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    [Obsolete("Use OpenCensus project packages")]
    internal static class Arrays
    {
        public static bool Equals(byte[] array1, byte[] array2)
        {
            if (array1 == array2)
            {
                return true;
            }

            if (array1 == null || array2 == null)
            {
                return false;
            }


            if (array2.Length != array1.Length)
            {
                return false;
            }
     

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;

        }

        internal static int GetHashCode(byte[] array)
        {
            if (array == null)
            {
                return 0;
            }
              

            int result = 1;
            foreach (byte b in array)
            {
                result = 31 * result + b;

            }

            return result;
        }

        internal static int HexCharToInt(char c)
        {
            if ((c >= '0') && (c <= '9'))
            {
                return (c - '0');
            }
            if ((c >= 'a') && (c <= 'f'))
            {
                return ((c - 'a') + 10);
            }
            if ((c >= 'A') && (c <= 'F'))
            {
                return ((c - 'A') + 10);
            }
            throw new ArgumentOutOfRangeException("Invalid character: " + c);
        }

        internal static char[] ByteToHexCharArray(byte b)
        {
            int low = b & 0x0f;
            int high = (b & 0xf0) >> 4;
            char[] result = new char[2];
            if (high > 9)
            {
                result[0] = (char) (high - 10 + 'a');
            } else
            {
                result[0] = (char)(high + '0');
            }
            if (low > 9)
            {
                result[1] = (char)(low - 10 + 'a');
            }
            else
            {
                result[1] = (char)(low + '0');
            }

            return result;
        }

        internal static byte[] StringToByteArray(string src)
        {
            int size = src.Length / 2;
            byte[] bytes = new byte[size];
            for (int i = 0, j = 0; i < size; i++)
            {
                int high = HexCharToInt(src[j++]);
                int low = HexCharToInt(src[j++]);
                bytes[i] = (byte)(high << 4 | low);
            }
            return bytes;
        }

        internal static string ByteArrayToString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(ByteToHexCharArray(bytes[i]));
            }
            return sb.ToString();
        }
    }
}
