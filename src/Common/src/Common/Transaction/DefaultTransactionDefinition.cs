// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
