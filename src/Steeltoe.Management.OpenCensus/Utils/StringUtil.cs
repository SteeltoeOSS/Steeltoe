using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    internal static class StringUtil
    {
        public static bool IsPrintableString(String str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!IsPrintableChar(str[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsPrintableChar(char ch)
        {
            return ch >= ' ' && ch <= '~';
        }
    }
}
