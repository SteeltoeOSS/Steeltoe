// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Util;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class SpelExpression : IExpression
{
    // Number of times to interpret an expression before compiling it
    internal const int InterpretedCountThreshold = 100;

    // Number of times to try compiling an expression before giving up
    internal const int FailedAttemptsThreshold = 100;

    private readonly object _lock = new();
    private readonly SpelNode _ast;
    private readonly SpelParserOptions _configuration;

    // Count of many times as the expression been interpreted - can trigger compilation
    // when certain limit reached
    private readonly AtomicInteger _interpretedCount = new(0);

    // The number of times compilation was attempted and failed - enables us to eventually
    // give up trying to compile it when it just doesn't seem to be possible.
    private readonly AtomicInteger _failedAttempts = new(0);

    // The default context is used if no override is supplied by the user
    private IEvaluationContext _evaluationContext;

    // Holds the compiled form of the expression (if it has been compiled)
    internal volatile CompiledExpression CompiledAst;

    public IEvaluationContext EvaluationContext
    {
        get
        {
            _evaluationContext ??= new StandardEvaluationContext();
            return _evaluationContext;
        }
        set => _evaluationContext = value;
    }

    // implementing Expression
    public string ExpressionString { get; }

    public ISpelNode Ast => _ast;

    public SpelExpression(string expression, SpelNode ast, SpelParserOptions configuration)
    {
        ExpressionString = expression;
        _ast = ast;
        _configuration = configuration;
    }

    public object GetValue()
    {
        if (CompiledAst != null)
        {
            try
            {
                IEvaluationContext context = EvaluationContext;
                return CompiledAst.GetValue(context.RootObject.Value, context);
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(EvaluationContext, _configuration);
        object result = _ast.GetValue(expressionState);
        CheckCompile(expressionState);
        return result;
    }

    public T GetValue<T>()
    {
        return (T)GetValue(typeof(T));
    }

    public object GetValue(Type desiredResultType)
    {
        if (CompiledAst != null)
        {
            try
            {
                IEvaluationContext context = EvaluationContext;
                object result = CompiledAst.GetValue(context.RootObject.Value, context);

                if (desiredResultType == null)
                {
                    return result;
                }

                return ExpressionUtils.ConvertTypedValue(EvaluationContext, new TypedValue(result), desiredResultType);
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(EvaluationContext, _configuration);
        ITypedValue typedResultValue = _ast.GetTypedValue(expressionState);
        CheckCompile(expressionState);
        return ExpressionUtils.ConvertTypedValue(expressionState.EvaluationContext, typedResultValue, desiredResultType);
    }

    public object GetValue(object rootObject)
    {
        if (CompiledAst != null)
        {
            try
            {
                return CompiledAst.GetValue(rootObject, EvaluationContext);
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration);
        object result = _ast.GetValue(expressionState);
        CheckCompile(expressionState);
        return result;
    }

    public T GetValue<T>(object rootObject)
    {
        return (T)GetValue(rootObject, typeof(T));
    }

    public object GetValue(object rootObject, Type desiredResultType)
    {
        if (CompiledAst != null)
        {
            try
            {
                object result = CompiledAst.GetValue(rootObject, EvaluationContext);

                if (desiredResultType == null)
                {
                    return result;
                }

                return ExpressionUtils.ConvertTypedValue(EvaluationContext, new TypedValue(result), desiredResultType);
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration);
        ITypedValue typedResultValue = _ast.GetTypedValue(expressionState);
        CheckCompile(expressionState);
        return ExpressionUtils.ConvertTypedValue(expressionState.EvaluationContext, typedResultValue, desiredResultType);
    }

    public object GetValue(IEvaluationContext context)
    {
        ArgumentGuard.NotNull(context);

        if (CompiledAst != null)
        {
            try
            {
                return CompiledAst.GetValue(context.RootObject.Value, context);
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(context, _configuration);
        object result = _ast.GetValue(expressionState);
        CheckCompile(expressionState);
        return result;
    }

    public T GetValue<T>(IEvaluationContext context)
    {
        return (T)GetValue(context, typeof(T));
    }

    public object GetValue(IEvaluationContext context, Type desiredResultType)
    {
        ArgumentGuard.NotNull(context);

        if (CompiledAst != null)
        {
            try
            {
                object result = CompiledAst.GetValue(context.RootObject.Value, context);

                if (desiredResultType != null)
                {
                    return ExpressionUtils.ConvertTypedValue(context, new TypedValue(result), desiredResultType);
                }

                return result;
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(context, _configuration);
        ITypedValue typedResultValue = _ast.GetTypedValue(expressionState);
        CheckCompile(expressionState);
        return ExpressionUtils.ConvertTypedValue(context, typedResultValue, desiredResultType);
    }

    public object GetValue(IEvaluationContext context, object rootObject)
    {
        ArgumentGuard.NotNull(context);

        if (CompiledAst != null)
        {
            try
            {
                return CompiledAst.GetValue(rootObject, context);
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(context, ToTypedValue(rootObject), _configuration);
        object result = _ast.GetValue(expressionState);
        CheckCompile(expressionState);
        return result;
    }

    public T GetValue<T>(IEvaluationContext context, object rootObject)
    {
        return (T)GetValue(context, rootObject, typeof(T));
    }

    public object GetValue(IEvaluationContext context, object rootObject, Type desiredResultType)
    {
        ArgumentGuard.NotNull(context);

        if (CompiledAst != null)
        {
            try
            {
                object result = CompiledAst.GetValue(rootObject, context);

                if (desiredResultType != null)
                {
                    return ExpressionUtils.ConvertTypedValue(context, new TypedValue(result), desiredResultType);
                }

                return result;
            }
            catch (Exception ex)
            {
                // If running in mixed mode, revert to interpreted
                if (_configuration.CompilerMode == SpelCompilerMode.Mixed)
                {
                    CompiledAst = null;
                    _interpretedCount.Value = 0;
                }
                else
                {
                    // Running in SpelCompilerMode.immediate mode - propagate exception to caller
                    throw new SpelEvaluationException(ex, SpelMessage.ExceptionRunningCompiledExpression);
                }
            }
        }

        var expressionState = new ExpressionState(context, ToTypedValue(rootObject), _configuration);
        ITypedValue typedResultValue = _ast.GetTypedValue(expressionState);
        CheckCompile(expressionState);
        return ExpressionUtils.ConvertTypedValue(context, typedResultValue, desiredResultType);
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
        ArgumentGuard.NotNull(context);

        var expressionState = new ExpressionState(context, _configuration);
        return _ast.GetValueInternal(expressionState).TypeDescriptor;
    }

    public Type GetValueType(IEvaluationContext context, object rootObject)
    {
        var expressionState = new ExpressionState(context, ToTypedValue(rootObject), _configuration);
        return _ast.GetValueInternal(expressionState).TypeDescriptor;
    }

    public bool IsWritable(object rootObject)
    {
        return _ast.IsWritable(new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration));
    }

    public bool IsWritable(IEvaluationContext context)
    {
        ArgumentGuard.NotNull(context);

        return _ast.IsWritable(new ExpressionState(context, _configuration));
    }

    public bool IsWritable(IEvaluationContext context, object rootObject)
    {
        ArgumentGuard.NotNull(context);

        return _ast.IsWritable(new ExpressionState(context, ToTypedValue(rootObject), _configuration));
    }

    public void SetValue(object rootObject, object value)
    {
        _ast.SetValue(new ExpressionState(EvaluationContext, ToTypedValue(rootObject), _configuration), value);
    }

    public void SetValue(IEvaluationContext context, object value)
    {
        ArgumentGuard.NotNull(context);

        _ast.SetValue(new ExpressionState(context, _configuration), value);
    }

    public void SetValue(IEvaluationContext context, object rootObject, object value)
    {
        ArgumentGuard.NotNull(context);

        _ast.SetValue(new ExpressionState(context, ToTypedValue(rootObject), _configuration), value);
    }

    public bool CompileExpression()
    {
        CompiledExpression compiledAst = CompiledAst;

        if (compiledAst != null)
        {
            // Previously compiled
            return true;
        }

        if (_failedAttempts.Value > FailedAttemptsThreshold)
        {
            // Don't try again
            return false;
        }

        lock (_lock)
        {
            if (CompiledAst != null)
            {
                // Compiled by another thread before this thread got into the sync block
                return true;
            }

            SpelCompiler compiler = SpelCompiler.GetCompiler();
            compiledAst = compiler.Compile(_ast);

            if (compiledAst != null)
            {
                // Successfully compiled
                CompiledAst = compiledAst;
                return true;
            }

            // Failed to compile
            _failedAttempts.IncrementAndGet();
            return false;
        }
    }

    public void RevertToInterpreted()
    {
        CompiledAst = null;
        _interpretedCount.Value = 0;
        _failedAttempts.Value = 0;
    }

    public string ToStringAst()
    {
        return _ast.ToStringAst();
    }

    private void CheckCompile(ExpressionState expressionState)
    {
        _interpretedCount.IncrementAndGet();
        SpelCompilerMode compilerMode = expressionState.Configuration.CompilerMode;

        if (compilerMode != SpelCompilerMode.Off)
        {
            if (compilerMode == SpelCompilerMode.Immediate)
            {
                if (_interpretedCount.Value > 1)
                {
                    CompileExpression();
                }
            }
            else
            {
                // compilerMode = SpelCompilerMode.MIXED
                if (_interpretedCount.Value > InterpretedCountThreshold)
                {
                    CompileExpression();
                }
            }
        }
    }

    private TypedValue ToTypedValue(object obj)
    {
        return obj != null ? new TypedValue(obj) : TypedValue.Null;
    }
}
