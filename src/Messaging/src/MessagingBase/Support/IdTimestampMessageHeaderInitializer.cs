// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Messaging.Support;

public class IdTimestampMessageHeaderInitializer : IMessageHeaderInitializer
{
    public IIDGenerator IdGenerator { get; set; }

    public bool EnableTimestamp { get; set; }

    public void SetDisableIdGeneration()
    {
        IdGenerator = new DisabledIDGenerator();
    }

    public void InitHeaders(IMessageHeaderAccessor headerAccessor)
    {
        var idGenerator = IdGenerator;
        if (idGenerator != null)
        {
            headerAccessor.IdGenerator = idGenerator;
        }

        headerAccessor.EnableTimestamp = EnableTimestamp;
    }

    internal class DisabledIDGenerator : IIDGenerator
    {
        public string GenerateId()
        {
            return MessageHeaders.ID_VALUE_NONE;
        }
    }
}