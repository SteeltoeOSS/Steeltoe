// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Spring.TestResources
{
    public class TestAddress
    {
        private string street;
        private List<string> crossStreets;

        public string Street
        {
            get => street;
            set => street = value;
        }

        public List<string> CrossStreets
        {
            get => crossStreets;
            set => crossStreets = value;
        }
    }
}
