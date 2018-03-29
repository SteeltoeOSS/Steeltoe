using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    internal static class DoubleUtil
    {
        public static long ToInt64(double arg)
        {

            if (Double.IsPositiveInfinity(arg))
            {
                return 0x7ff0000000000000L;
            }
            if (Double.IsNegativeInfinity(arg))
            {
                unchecked
                {
                    return (long)0xfff0000000000000L;
                }
            }
            if (Double.IsNaN(arg))
            {
                return 0x7ff8000000000000L;
            }
            return Convert.ToInt64(arg);

        }
    }
}
