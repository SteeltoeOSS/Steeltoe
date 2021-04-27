﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler
{
    public abstract class AbstractMessageProcessor<T> : AbstractExpressionEvaluator, IMessageProcessor<T>
    {
        protected AbstractMessageProcessor(IApplicationContext context)
            : base(context)
        {
        }

        public abstract T ProcessMessage(IMessage message);

        object IMessageProcessor.ProcessMessage(IMessage message)
        {
            return this.ProcessMessage(message);
        }
    }
}
