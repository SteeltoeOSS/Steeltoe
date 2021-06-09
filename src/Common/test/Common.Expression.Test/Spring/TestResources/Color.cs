// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources
{
    public class Color
    {
        private Color(int rgb)
        {
            RGB = rgb;
        }

        public int RGB { get; }

        public static Color Orange = new (1);
        public static Color Yellow = new (2);
        public static Color Green = new (3);
        public static Color Red = new (4);
        public static Color Blue = new (5);
    }
}
