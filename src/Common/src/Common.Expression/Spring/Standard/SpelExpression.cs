// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard
{
    public class SpelExpression : IExpression
    {
        // Number of times to interpret an expression before compiling it
        internal const int _INTERPRETED_COUNT_THRESHOLD = 100;

        // Number of times to try compiling an expression before giving up
        internal const int _FAILED_ATTEMPTS_THRESHOLD = 100;

        // Holds the compiled form of the expression (if it has been compiled)
        internal volatile CompiledExpression _compiledAst;

        private readonly object _lock = new object();
        private readonly string _expression;
        private readonly SpelNode _ast;
        private readonly SpelParserOptions _configuration;

        // Count of many times as the expression been interpreted - can trigger compilation
        // when certain limit reached
        private readonly AtomicInteger _interpretedCount = new AtomicInteger(0);

        // The number of times compilation was attempted and failed - enables us to eventually
        // give up trying to compile it when it just doesn't seem to be possible.
        private readonly AtomicInteger _failedAttempts = new AtomicInteger(0);

        // The default context is used if no override is supplied by the user
        private IEvaluationContext _evaluationContext;

        public SpelExpression(string expression, SpelNode ast, SpelParserOptions configuration)
        {
            _expression = expression;
            _ast = ast;
            _configuration = configuration;
        }

        public IEvaluationContext EvaluationContext
        {
            get
            {
                if (_evaluationContext == null)
                {
                    _evaluationContext = new StandardEvaluationContext();
                }

                return _evaluationContext;
            }

            set => _evaluationContext = value;
        }

        // implementing Expression
        public string ExpressionString => _expression;

        public object GetValue()
        {
            if (_compiledAst != null)
            {
                try
                {
                    var context = EvaluationContext;
                    return _compiledAst.GetValue(context.RootObject.Value, context);
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(EvaluationContext, _configuration);
            var result = _ast.GetValue(expressionState);
            CheckCompile(expressionState);
            return result;
        }

        public T GetValue<T>()
        {
            return (T)GetValue(typeof(T));
        }

        public object GetValue(Type expectedResultType)
        {
            if (_compiledAst != null)
            {
                try
                {
                    var context = EvaluationContext;
                    var result = _compiledAst.GetValue(context.RootObject.Value, context);
                    if (expectedResultType == null)
                    {
                        return result;
                    }
                    else
                    {
                        return ExpressionUtils.ConvertTypedValue(EvaluationContext, new TypedValue(result), expectedResultType);
                    }
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(EvaluationContext, _configuration);
            var typedResultValue = _ast.GetTypedValue(expressionState);
            CheckCompile(expressionState);
            return ExpressionUtils.ConvertTypedValue(expressionState.EvaluationContext, typedResultValue, expectedResultType);
        }

        public object GetValue(object rootObject)
        {
            if (_compiledAst != null)
            {
                try
                {
                    return _compiledAst.GetValue(rootObject, EvaluationContext);
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration);
            var result = _ast.GetValue(expressionState);
            CheckCompile(expressionState);
            return result;
        }

        public T GetValue<T>(object rootObject)
        {
            return (T)GetValue(rootObject, typeof(T));
        }

        public object GetValue(object rootObject, Type expectedResultType)
        {
            if (_compiledAst != null)
            {
                try
                {
                    var result = _compiledAst.GetValue(rootObject, EvaluationContext);
                    if (expectedResultType == null)
                    {
                        return result;
                    }
                    else
                    {
                        return ExpressionUtils.ConvertTypedValue(EvaluationContext, new TypedValue(result), expectedResultType);
                    }
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration);
            var typedResultValue = _ast.GetTypedValue(expressionState);
            CheckCompile(expressionState);
            return ExpressionUtils.ConvertTypedValue(expressionState.EvaluationContext, typedResultValue, expectedResultType);
        }

        public object GetValue(IEvaluationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_compiledAst != null)
            {
                try
                {
                    return _compiledAst.GetValue(context.RootObject.Value, context);
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(context, _configuration);
            var result = _ast.GetValue(expressionState);
            CheckCompile(expressionState);
            return result;
        }

        public T GetValue<T>(IEvaluationContext context)
        {
            return (T)GetValue(context, typeof(T));
        }

        public object GetValue(IEvaluationContext context, Type expectedResultType)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_compiledAst != null)
            {
                try
                {
                    var result = _compiledAst.GetValue(context.RootObject.Value, context);
                    if (expectedResultType != null)
                    {
                        return ExpressionUtils.ConvertTypedValue(context, new TypedValue(result), expectedResultType);
                    }
                    else
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(context, _configuration);
            var typedResultValue = _ast.GetTypedValue(expressionState);
            CheckCompile(expressionState);
            return ExpressionUtils.ConvertTypedValue(context, typedResultValue, expectedResultType);
        }

        public object GetValue(IEvaluationContext context, object rootObject)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_compiledAst != null)
            {
                try
                {
                    return _compiledAst.GetValue(rootObject, context);
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(context, ToTypedValue(rootObject), _configuration);
            var result = _ast.GetValue(expressionState);
            CheckCompile(expressionState);
            return result;
        }

        public T GetValue<T>(IEvaluationContext context, object rootObject)
        {
            return (T)GetValue(context, rootObject, typeof(T));
        }

        public object GetValue(IEvaluationContext context, object rootObject, Type expectedResultType)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_compiledAst != null)
            {
                try
                {
                    var result = _compiledAst.GetValue(rootObject, context);
                    if (expectedResultType != null)
                    {
                        return ExpressionUtils.ConvertTypedValue(context, new TypedValue(result), expectedResultType);
                    }
                    else
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    // If running in mixed mode, revert to interpreted
                    if (_configuration.CompilerMode == SpelCompilerMode.MIXED)
                    {
                        _compiledAst = null;
                        _interpretedCount.Value = 0;
                    }
                    else
                    {
                        // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                        throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_RUNNING_COMPILED_EXPRESSION);
                    }
                }
            }

            var expressionState = new ExpressionState(context, ToTypedValue(rootObject), _configuration);
            var typedResultValue = _ast.GetTypedValue(expressionState);
            CheckCompile(expressionState);
            return ExpressionUtils.ConvertTypedValue(context, typedResultValue, expectedResultType);
        }

        public Type GetValueType()
        {
            return GetValueType(EvaluationContext);
        }

        public Type GetValueType(object rootObject)
        {
            return GetValueType(EvaluationContext, rootObject);
        }

        public Type GetValueType(IEvaluationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var expressionState = new ExpressionState(context, _configuration);
            return _ast.GetValueInternal(expressionState).TypeDescriptor;
        }

        public Type GetValueType(IEvaluationContext context, object rootObject)
        {
            var expressionState = new ExpressionState(context, ToTypedValue(rootObject), _configuration);
            return _ast.GetValueInternal(expressionState).TypeDescriptor;
        }

        // public Type GetValueTypeDescriptor()
        // {
        //    return GetValueTypeDescriptor(EvaluationContext);
        // }

        // public Type GetValueTypeDescriptor(object rootObject)
        // {
        //    var expressionState = new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration);
        //    return _ast.GetValueInternal(expressionState).TypeDescriptor;
        // }

        // public Type GetValueTypeDescriptor(IEvaluationContext context)
        // {
        //    if (context == null)
        //    {
        //        throw new ArgumentNullException(nameof(context));
        //    }

        // var expressionState = new ExpressionState(context, _configuration);
        //    return _ast.GetValueInternal(expressionState).TypeDescriptor;
        // }

        // public Type GetValueTypeDescriptor(IEvaluationContext context, object rootObject)
        // {
        //    if (context == null)
        //    {
        //        throw new ArgumentNullException(nameof(context));
        //    }

        // var expressionState = new ExpressionState(context, ToTypedValue(rootObject), _configuration);
        //    return _ast.GetValueInternal(expressionState).TypeDescriptor;
        // }
        public bool IsWritable(object rootObject)
        {
            return _ast.IsWritable(new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration));
        }

        public bool IsWritable(IEvaluationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _ast.IsWritable(new ExpressionState(context, _configuration));
        }

        public bool IsWritable(IEvaluationContext context, object rootObject)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _ast.IsWritable(new ExpressionState(context, ToTypedValue(rootObject), _configuration));
        }

        public void SetValue(object rootObject, object value)
        {
            _ast.SetValue(new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration), value);
        }

        public void SetValue(IEvaluationContext context, object value)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _ast.SetValue(new ExpressionState(context, _configuration), value);
        }

        public void SetValue(IEvaluationContext context, object rootObject, object value)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _ast.SetValue(new ExpressionState(context, ToTypedValue(rootObject), _configuration), value);
        }

        public bool CompileExpression()
        {
            var compiledAst = _compiledAst;
            if (compiledAst != null)
            {
                // Previously compiled
                return true;
            }

            if (_failedAttempts.Value > _FAILED_ATTEMPTS_THRESHOLD)
            {
                // Don't try again
                return false;
            }

            lock (_lock)
            {
                if (_compiledAst != null)
                {
                    // Compiled by another thread before this thread got into the sync block
                    return true;
                }

                var compiler = SpelCompiler.GetCompiler();
                compiledAst = compiler.Compile(_ast);
                if (compiledAst != null)
                {
                    // Successfully compiled
                    _compiledAst = compiledAst;
                    return true;
                }
                else
                {
                    // Failed to compile
                    _failedAttempts.IncrementAndGet();
                    return false;
                }
            }
        }

        public void RevertToInterpreted()
        {
            _compiledAst = null;
            _interpretedCount.Value = 0;
            _failedAttempts.Value = 0;
        }

        public ISpelNode AST => _ast;

        public string ToStringAST()
        {
            return _ast.ToStringAST();
        }

        private void CheckCompile(ExpressionState expressionState)
        {
            _interpretedCount.IncrementAndGet();
            var compilerMode = expressionState.Configuration.CompilerMode;
            if (compilerMode != SpelCompilerMode.OFF)
            {
                if (compilerMode == SpelCompilerMode.IMMEDIATE)
                {
                    if (_interpretedCount.Value > 1)
                    {
                        CompileExpression();
                    }
                }
                else
                {
                    // compilerMode = SpelCompilerMode.MIXED
                    if (_interpretedCount.Value > _INTERPRETED_COUNT_THRESHOLD)
                    {
                        CompileExpression();
                    }
                }
            }
        }

        private TypedValue ToTypedValue(object obj)
        {
            return obj != null ? new TypedValue(obj) : TypedValue.NULL;
        }
    }
}
