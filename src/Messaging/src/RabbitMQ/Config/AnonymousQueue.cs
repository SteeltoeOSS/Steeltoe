﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Core;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class AnonymousQueue : Queue
    {
        public AnonymousQueue(string name)
            : base(name, false, true, true, null)
        {
        }

        public AnonymousQueue(Dictionary<string, object> arguments)
            : this(Base64UrlNamingStrategy.DEFAULT, arguments)
        {
        }

        public AnonymousQueue(INamingStrategy namingStrategy)
            : this(namingStrategy, null)
        {
        }

        public AnonymousQueue(INamingStrategy namingStrategy, Dictionary<string, object> arguments)
            : base(namingStrategy.GenerateName(), false, true, true, arguments)
        {
            if (!Arguments.ContainsKey(Queue.X_QUEUE_MASTER_LOCATOR))
            {
                MasterLocator = "client-local";
            }
        }
    }
}
