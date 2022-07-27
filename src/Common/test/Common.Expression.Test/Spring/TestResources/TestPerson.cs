// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources;

public class TestPerson
{
    private string name;
    private TestAddress address;

    public string Name
    {
        get => name;
        set => name = value;
    }

    public TestAddress Address
    {
        get => address;
        set => address = value;
    }
}