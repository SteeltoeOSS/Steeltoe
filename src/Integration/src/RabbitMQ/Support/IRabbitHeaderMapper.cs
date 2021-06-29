// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Mapping;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Integration.Rabbit.Support
{
    public interface IRabbitHeaderMapper : IRequestReplyHeaderMapper<IMessageHeaders>
    {
    }
}
