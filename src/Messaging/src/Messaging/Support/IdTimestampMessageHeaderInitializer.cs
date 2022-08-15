// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Support;

public class IdTimestampMessageHeaderInitializer : IMessageHeaderInitializer
{
    public IIdGenerator IdGenerator { get; set; }

    public bool EnableTimestamp { get; set; }

    public void SetDisableIdGeneration()
    {
        IdGenerator = new DisabledIdGenerator();
    }

    public void InitHeaders(IMessageHeaderAccessor headerAccessor)
    {
        IIdGenerator idGenerator = IdGenerator;

        if (idGenerator != null)
        {
            headerAccessor.IdGenerator = idGenerator;
        }

        headerAccessor.EnableTimestamp = EnableTimestamp;
    }

    internal sealed class DisabledIdGenerator : IIdGenerator
    {
        public string GenerateId()
        {
            return MessageHeaders.IdValueNone;
        }
    }
}
