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
using System.Text;

namespace Steeltoe.Common.Transaction
{
    public class DefaultTransactionAttribute : DefaultTransactionDefinition, ITransactionAttribute
    {
        public DefaultTransactionAttribute()
            : base()
        {
        }

        public DefaultTransactionAttribute(ITransactionAttribute other)
            : base(other)
        {
        }

        public DefaultTransactionAttribute(int propagationBehavior)
            : base(propagationBehavior)
        {
        }

        public string Qualifier { get; }

        public string Descriptor { get; }

        public bool RollbackOn(Exception exception)
        {
            return false; // TODO: Not sure on this??
        }

        protected StringBuilder GetAttributeDescription()
        {
            var result = GetDefinitionDescription();
            if (!string.IsNullOrEmpty(Qualifier))
            {
                result.Append("; '").Append(Qualifier).Append("'");
            }

            return result;
        }
    }
}
