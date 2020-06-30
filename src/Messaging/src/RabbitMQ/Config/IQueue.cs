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

using Steeltoe.Common.Services;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public interface IQueue : IDeclarable, IServiceNameAware
    {
        string QueueName { get; set; }

        string ActualName { get; set; }

        bool IsDurable { get; set; }

        bool IsExclusive { get; set; }

        bool IsAutoDelete { get; set; }

        string MasterLocator { get; set; }

        object Clone();
    }
}
