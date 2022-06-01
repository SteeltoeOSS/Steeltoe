// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources;

public class Fruit
{
    public string Name; // accessible as property field
    public string ColorName; // accessible as property through getter/setter

    public Fruit(string name, Color color, string colorName)
    {
        Name = name;
        Color = color;
        ColorName = colorName;
    }

    public Color Color { get; }

    public Fruit(params string[] strings)
    {
        StringsCount = strings.Length;
    }

    public Fruit(int i, params string[] strings)
    {
        StringsCount = i + strings.Length;
    }

    public int StringsCount { get; } = -1;

    public override string ToString()
    {
        return $"A{(ColorName != null && ColorName.StartsWith("o") ? "n " : " ")}{ColorName} {Name}";
    }
}
