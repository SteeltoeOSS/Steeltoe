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

using System.Collections;
using System.Text;

namespace Steeltoe.Messaging.Handler
{
    public abstract class AbstractMessageCondition<T> : IMessageCondition<T>
    {
        public abstract T Combine(T other);

        public abstract int CompareTo(T other, IMessage message);

        public abstract T GetMatchingCondition(IMessage message);

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }

            if (other == null || GetType() != other.GetType())
            {
                return false;
            }

            var content1 = GetContent();
            var content2 = GetContent();

            if (content1.Count != content2.Count)
            {
                return false;
            }

            for (var i = 0; i < content1.Count; i++)
            {
                if (!content1[i].Equals(content2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return GetContent().GetHashCode();
        }

        public override string ToString()
        {
            var infix = GetToStringInfix();

            var joiner = new StringBuilder("[");
            foreach (var expression in GetContent())
            {
                joiner.Append(expression.ToString() + infix);
            }

            return joiner.ToString(0, joiner.Length - 1) + "]";
        }

        protected abstract IList GetContent();

        protected abstract string GetToStringInfix();
    }
}
