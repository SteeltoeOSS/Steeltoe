// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources;

public class Person
{
    public string Name { get; set; }

    public Company Company { get; }

    public Person(string name)
    {
        Name = name;
    }

    public Person(string name, Company company)
    {
        Name = name;
        Company = company;
    }
}
