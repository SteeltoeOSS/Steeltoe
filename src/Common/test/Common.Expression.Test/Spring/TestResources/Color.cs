// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources;

public class Color
{
    public static Color Orange { get; } = new(1);
    public static Color Yellow { get; } = new(2);
    public static Color Green { get; } = new(3);
    public static Color Red { get; } = new(4);
    public static Color Blue { get; } = new(5);

    public int Rgb { get; }

    private Color(int rgb)
    {
        Rgb = rgb;
    }
}
