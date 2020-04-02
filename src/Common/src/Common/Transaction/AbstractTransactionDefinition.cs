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

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Transaction
{
    public abstract class AbstractTransactionDefinition : ITransactionDefinition
    {
        public const int PROPAGATION_REQUIRED = 0;
        public const int PROPAGATION_SUPPORTS = 1;
        public const int PROPAGATION_MANDATORY = 2;
        public const int PROPAGATION_REQUIRES_NEW = 3;
        public const int PROPAGATION_NOT_SUPPORTED = 4;
        public const int PROPAGATION_NEVER = 5;
        public const int PROPAGATION_NESTED = 6;
        public const int ISOLATION_DEFAULT = -1;
        public const int ISOLATION_READ_UNCOMMITTED = 1;
        public const int ISOLATION_READ_COMMITTED = 2;
        public const int ISOLATION_REPEATABLE_READ = 4;
        public const int ISOLATION_SERIALIZABLE = 8;
        public const int TIMEOUT_DEFAULT = -1;

        public static ITransactionDefinition WithDefaults => StaticTransactionDefinition.INSTANCE;

        public virtual int PropagationBehavior { get; set; } = PROPAGATION_REQUIRED;

        public virtual int IsolationLevel { get; set; } = ISOLATION_DEFAULT;

        public virtual int Timeout { get; set; } = TIMEOUT_DEFAULT;

        public virtual bool IsReadOnly { get; set; } = false;

        public virtual string Name { get; set; } = null;
    }
}
