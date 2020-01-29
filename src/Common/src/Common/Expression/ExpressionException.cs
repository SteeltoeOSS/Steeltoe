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

namespace Steeltoe.Common.Expression
{
    public class ExpressionException : Exception
    {
        protected string ExpressionString { get; set; }

        protected int Position { get; set; }

        public ExpressionException(string message)
        : base(message)
        {
            ExpressionString = null;
            Position = 0;
        }

        public ExpressionException(string message, Exception cause)
        : base(message, cause)
        {
            ExpressionString = null;
            Position = 0;
        }

        public ExpressionException(string expressionString, string message)
        : base(message)
        {
            ExpressionString = expressionString;
            Position = -1;
        }

        public ExpressionException(string expressionString, int position, string message)
        : base(message)
        {
            ExpressionString = expressionString;
            Position = position;
        }

        public ExpressionException(int position, string message)
        : base(message)
        {
            ExpressionString = null;
            Position = position;
        }

        public ExpressionException(int position, string message, Exception cause)
            : base(message, cause)
        {
            ExpressionString = null;
            Position = position;
        }
    }
}
