// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources
{
    public class Person
    {
        private string privateName;
        private Company company;

        public Person(string name)
        {
            this.privateName = name;
        }

        public Person(string name, Company company)
        {
            this.privateName = name;
            this.company = company;
        }

        public string Name
        {
            get => privateName;
            set => privateName = value;
        }

        public Company Company => company;
    }
}
