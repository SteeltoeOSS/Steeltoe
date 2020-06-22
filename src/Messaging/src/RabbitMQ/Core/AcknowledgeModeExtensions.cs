﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Rabbit.Core
{
    public static class AcknowledgeModeExtensions
    {
        public static bool IsTransactionAllowed(this AcknowledgeMode mode)
        {
            return mode == AcknowledgeMode.AUTO || mode == AcknowledgeMode.MANUAL;
        }

        public static bool IsAutoAck(this AcknowledgeMode mode)
        {
            return mode == AcknowledgeMode.NONE;
        }

        public static bool IsManual(this AcknowledgeMode mode)
        {
            return mode == AcknowledgeMode.MANUAL;
        }
    }
}
