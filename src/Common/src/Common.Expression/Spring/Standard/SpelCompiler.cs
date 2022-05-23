// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using System;
using System.Reflection.Emit;
using static Steeltoe.Common.Expression.Internal.Spring.Standard.SpelCompiledExpression;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard
{
    public class SpelCompiler
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SpelCompiler> _logger;

        // Counter suffix for generated classes within this SpelCompiler instance
        private readonly AtomicInteger _suffixId = new (1);

        public static SpelCompiler GetCompiler(ILoggerFactory loggerFactory = null)
        {
            return new SpelCompiler(loggerFactory);
        }

        public static void RevertToInterpreted(IExpression expression)
        {
            if (expression is SpelExpression expression1)
            {
                expression1.RevertToInterpreted();
            }
        }

        public static bool Compile(IExpression expression)
        {
            return expression is SpelExpression expression1 && expression1.CompileExpression();
        }

        public CompiledExpression Compile(ISpelNode expression)
        {
            if (expression.IsCompilable())
            {
                _logger?.LogDebug("SpEL: compiling {expression}", expression.ToStringAST());
                return CreateExpressionClass(expression);
            }

            _logger?.LogDebug("SpEL: unable to compile {expression} ", expression.ToStringAST());
            return null;
        }

        private SpelCompiler(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<SpelCompiler>();
        }

        private CompiledExpression CreateExpressionClass(ISpelNode expressionToCompile)
        {
            var compiledExpression = new SpelCompiledExpression(_loggerFactory);
            var methodName = $"SpelExpression{_suffixId.GetAndIncrement()}";
            var method = new DynamicMethod(methodName, typeof(object), new Type[] { typeof(SpelCompiledExpression), typeof(object), typeof(IEvaluationContext) }, typeof(SpelCompiledExpression));
            var ilGenerator = method.GetILGenerator(4096);
            var cf = new CodeFlow(compiledExpression);
            try
            {
                expressionToCompile.GenerateCode(ilGenerator, cf);

                var lastDescriptor = cf.LastDescriptor();
                CodeFlow.InsertBoxIfNecessary(ilGenerator, lastDescriptor);
                if (lastDescriptor == TypeDescriptor.V)
                {
                    ilGenerator.Emit(OpCodes.Ldnull);
                }

                ilGenerator.Emit(OpCodes.Ret);
                compiledExpression.MethodDelegate = method.CreateDelegate(typeof(SpelExpressionDelegate));
                var initMethod = cf.Finish(_suffixId.Value);
                if (initMethod != null)
                {
                    compiledExpression.InitDelegate = initMethod.CreateDelegate(typeof(SpelExpressionInitDelegate));
                }

                return compiledExpression;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(expressionToCompile.GetType().Name + ".GenerateCode opted out of compilation: " + ex.Message);
                return null;
            }
        }
    }
}
