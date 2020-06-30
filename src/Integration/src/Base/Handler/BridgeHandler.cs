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

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Handler
{
    public class BridgeHandler : AbstractReplyProducingMessageHandler
    {
        public BridgeHandler(IApplicationContext context)
            : base(context)
        {
        }

        public override string ComponentType
        {
            get { return "bridge"; }
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            return requestMessage;
        }

        protected override bool ShouldCopyRequestHeaders
        {
            get { return false; }
        }
    }
}
