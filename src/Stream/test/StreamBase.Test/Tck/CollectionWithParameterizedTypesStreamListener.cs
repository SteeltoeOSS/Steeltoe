// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Messaging;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Stream.Tck
{
    public class CollectionWithParameterizedTypesStreamListener
    {
        [StreamListener(ISink.INPUT)]
        [SendTo(ISource.OUTPUT)]
        public List<Employee<Person>> Echo(List<Employee<Person>> value)
        {
            Assert.True(value.Count > 0);
            Assert.NotNull(value[0]);
            return value;
        }
    }
}
