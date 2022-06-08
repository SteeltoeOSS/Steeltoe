// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class SpelCompiledExpression : CompiledExpression
{
    private readonly ILogger<SpelCompiledExpression> _logger;

    private int _initialized;

    internal delegate object SpelExpressionDelegate(SpelCompiledExpression expression, object target, IEvaluationContext context);

    internal delegate void SpelExpressionInitDelegate(SpelCompiledExpression expression, object target, IEvaluationContext context);

    internal SpelCompiledExpression(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<SpelCompiledExpression>();
    }

    public override object GetValue(object target, IEvaluationContext context)
    {
        var initDelegate = InitDelegate as SpelExpressionInitDelegate;
        var spelDelegate = MethodDelegate as SpelExpressionDelegate;
        try
        {
            // One time initialization call that allows expression init if needed (e.g. InlineList uses this)
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                initDelegate?.Invoke(this, target, context);
            }

            // Invoke the compiled expression
            var result = spelDelegate.Invoke(this, target, context);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Compiled Expression exception");
            throw;
        }
    }
}
