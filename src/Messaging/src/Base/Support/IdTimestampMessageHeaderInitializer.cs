// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Messaging.Support
{
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
}
