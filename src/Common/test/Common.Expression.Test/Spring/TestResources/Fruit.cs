// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources;

public class Fruit
{
    public string Name; // accessible as property field
    public string ColorName; // accessible as property through getter/setter
    private Color _color; // accessible as property through getter/setter
    private int stringscount = -1;

    public Fruit(string name, Color color, string colorName)
    {
        Name = name;
        _color = color;
        ColorName = colorName;
    }

    public Color Color => _color;

    public Fruit(params string[] strings)
    {
        stringscount = strings.Length;
    }

    public Fruit(int i, params string[] strings)
    {
        stringscount = i + strings.Length;
    }

    public int StringsCount => stringscount;

    public override string ToString()
    {
        return "A" + (ColorName != null && ColorName.StartsWith("o") ? "n " : " ") + ColorName + " " + Name;
    }
}