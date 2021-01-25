// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class ServiceReference : SpelNode
    {
        private readonly string _serviceName;

        public ServiceReference(int startPos, int endPos, string serviceName)
            : base(startPos, endPos)
        {
            _serviceName = serviceName;
        }

        public override ITypedValue GetValueInternal(ExpressionState expressionState)
        {
            var serviceResolver = expressionState.EvaluationContext.ServiceResolver;
            if (serviceResolver == null)
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.NO_SERVICE_RESOLVER_REGISTERED, _serviceName);
            }

            try
            {
                return new TypedValue(serviceResolver.Resolve(expressionState.EvaluationContext, _serviceName));
            }
            catch (AccessException ex)
            {
                throw new SpelEvaluationException(StartPosition, ex, SpelMessage.EXCEPTION_DURING_SERVICE_RESOLUTION, _serviceName, ex.Message);
            }
        }

        public override string ToStringAST()
        {
            var sb = new StringBuilder();
            sb.Append("@");
            if (!_serviceName.Contains("."))
            {
                sb.Append(_serviceName);
            }
            else
            {
                sb.Append("'").Append(_serviceName).Append("'");
            }

            return sb.ToString();
        }
    }
}
