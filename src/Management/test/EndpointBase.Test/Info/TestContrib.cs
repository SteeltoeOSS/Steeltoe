// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    internal class TestContrib : IInfoContributor
    {
#pragma warning disable SA1401 // Fields must be private
        public bool Called = false;
        public bool Throws = false;
#pragma warning restore SA1401 // Fields must be private

        public TestContrib()
        {
            this.Throws = false;
        }

        public TestContrib(bool throws)
        {
            this.Throws = throws;
        }

        public void Contribute(IInfoBuilder builder)
        {
            if (Throws)
            {
                throw new Exception();
            }

            Called = true;
        }
    }
}
