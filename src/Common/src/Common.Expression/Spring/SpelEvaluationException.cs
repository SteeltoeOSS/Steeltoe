// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Spring
{
    public class SpelEvaluationException : EvaluationException
    {
        public SpelMessage MessageCode { get; }

        public object[] Inserts { get; }

        public SpelEvaluationException(SpelMessage message, params object[] inserts)
            : base(message.FormatMessage(inserts))
        {
            MessageCode = message;
            Inserts = inserts;
        }

        public SpelEvaluationException(int position, SpelMessage message, params object[] inserts)
            : base(position, message.FormatMessage(inserts))
        {
            MessageCode = message;
            Inserts = inserts;
        }

        public SpelEvaluationException(int position, Exception cause, SpelMessage message, params object[] inserts)
            : base(position, message.FormatMessage(inserts), cause)
        {
            MessageCode = message;
            Inserts = inserts;
        }

        public SpelEvaluationException(Exception cause, SpelMessage message, params object[] inserts)
            : base(message.FormatMessage(inserts), cause)
        {
            MessageCode = message;
            Inserts = inserts;
        }
    }
}
