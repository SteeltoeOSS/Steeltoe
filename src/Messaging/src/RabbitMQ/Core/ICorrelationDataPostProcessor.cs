﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Data;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public interface ICorrelationDataPostProcessor
    {
        CorrelationData PostProcess(Message message, CorrelationData correlationData);
    }
}
