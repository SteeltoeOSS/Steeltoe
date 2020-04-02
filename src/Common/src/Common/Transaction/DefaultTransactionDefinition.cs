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

using System.Text;

namespace Steeltoe.Common.Transaction
{
    public class DefaultTransactionDefinition : AbstractTransactionDefinition
    {
        public const string PREFIX_PROPAGATION = "PROPAGATION_";
        public const string PREFIX_ISOLATION = "ISOLATION_";
        public const string PREFIX_TIMEOUT = "timeout_";
        public const string READ_ONLY_MARKER = "readOnly";

        public DefaultTransactionDefinition()
        {
        }

        public DefaultTransactionDefinition(ITransactionDefinition other)
        {
            PropagationBehavior = other.PropagationBehavior;
            IsolationLevel = other.IsolationLevel;
            Timeout = other.Timeout;
            IsReadOnly = other.IsReadOnly;
            Name = other.Name;
        }

        public DefaultTransactionDefinition(int propagationBehavior)
        {
            PropagationBehavior = propagationBehavior;
        }

        public override int PropagationBehavior { get; set; }

        public override int IsolationLevel { get; set; }

        public override int Timeout { get; set; }

        public override bool IsReadOnly { get; set; }

        public override string Name { get; set; }

        public override bool Equals(object other)
        {
            return this == other || (other is ITransactionDefinition && ToString().Equals(other.ToString()));
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return GetDefinitionDescription().ToString();
        }

        protected StringBuilder GetDefinitionDescription()
        {
            var result = new StringBuilder();
            result.Append(PropagationBehavior);
            result.Append(',');
            result.Append(IsolationLevel);
            if (Timeout != TIMEOUT_DEFAULT)
            {
                result.Append(',');
                result.Append(PREFIX_TIMEOUT).Append(Timeout);
            }

            if (IsReadOnly)
            {
                result.Append(',');
                result.Append(READ_ONLY_MARKER);
            }

            return result;
        }
    }
}
