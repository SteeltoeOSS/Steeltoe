// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Steeltoe.Integration.Expression;

public class ExpressionEvalDictionary : IDictionary<string, object>
{
    private readonly IDictionary<string, IExpression> _original;
    private readonly IEvaluationCallback _evaluationCallback;

    private ExpressionEvalDictionary(IDictionary<string, IExpression> original, IEvaluationCallback evaluationCallback = null)
    {
        _original = original;
        _evaluationCallback = evaluationCallback;
    }

    public static ExpressionEvalDictionaryBuilder From(IDictionary<string, IExpression> expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException(nameof(expressions));
        }

        return new ExpressionEvalDictionaryBuilder(expressions);
    }

    public object this[string key] { get => Get(key); set => throw new NotImplementedException(); }

    public ICollection<string> Keys => _original.Keys;

    public ICollection<object> Values
    {
        get
        {
            var list = new List<object>(_original.Count);
            var keys = _original.Keys;
            foreach (var key in keys)
            {
                list.Add(Get(key));
            }

            return list;
        }
    }

    public int Count => _original.Count;

    public bool IsReadOnly => true;

    public void Add(string key, object value)
    {
        throw new InvalidOperationException();
    }

    public void Add(KeyValuePair<string, object> item)
    {
        throw new InvalidOperationException();
    }

    public void Clear()
    {
        throw new InvalidOperationException();
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
        throw new InvalidOperationException();
    }

    public bool ContainsKey(string key)
    {
        return _original.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        throw new InvalidOperationException();
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        var results = new List<KeyValuePair<string, object>>();
        foreach (var entry in _original)
        {
            var value = Get(entry.Key);
            results.Add(new KeyValuePair<string, object>(entry.Key, value));
        }

        return results.GetEnumerator();
    }

    public bool Remove(string key)
    {
        throw new InvalidOperationException();
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        if (!ContainsKey(key))
        {
            value = null;
            return false;
        }

        value = Get(key);
        return true;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new InvalidOperationException();
    }

    public object Get(string key)
    {
        _original.TryGetValue(key, out var expression);
        if (expression != null)
        {
            return _evaluationCallback.Evaluate(expression);
        }

        return null;
    }

    public class ComponentsEvaluationCallback : IEvaluationCallback
    {
        private readonly IEvaluationContext _context;

        private readonly object _root;

        private readonly bool _rootExplicitlySet;

        private readonly Type _returnType;

        public ComponentsEvaluationCallback(IEvaluationContext context, object root, bool rootExplicitlySet, Type returnType)
        {
            _context = context;
            _root = root;
            _rootExplicitlySet = rootExplicitlySet;
            _returnType = returnType;
        }

        public object Evaluate(IExpression expression)
        {
            if (_context != null)
            {
                if (_rootExplicitlySet)
                {
                    return expression.GetValue(_context, _root, _returnType);
                }
                else
                {
                    return expression.GetValue(_context, _returnType);
                }
            }

            return expression.GetValue(_root, _returnType);
        }
    }

    public class SimpleCallback : IEvaluationCallback
    {
        public object Evaluate(IExpression expression)
        {
            return expression.GetValue();
        }
    }

    public class ExpressionEvalDictionaryBuilder
    {
        private static readonly IEvaluationCallback SimpleCallback = new SimpleCallback();

        private readonly IExpressionEvalMapComponentsBuilder _evalMapComponentsBuilder;

        private readonly IExpressionEvalMapFinalBuilder _finalBuilder;

        private IDictionary<string, IExpression> Expressions { get; }

        private IEvaluationContext EvaluationContext { get; set; }

        private object Root { get; set; }

        private bool RootExplicitlySet { get; set; }

        private Type ReturnType { get; set; }

        private IEvaluationCallback EvaluationCallback { get; set; }

        public ExpressionEvalDictionaryBuilder(IDictionary<string, IExpression> expressions)
        {
            Expressions = expressions;
            _finalBuilder = new ExpressionEvalMapFinalBuilderImpl(this);
            _evalMapComponentsBuilder = new ExpressionEvalMapComponentsBuilderImpl(this);
        }

        public IExpressionEvalMapFinalBuilder UsingCallback(IEvaluationCallback callback)
        {
            EvaluationCallback = callback;
            return _finalBuilder;
        }

        public IExpressionEvalMapFinalBuilder UsingSimpleCallback()
        {
            return UsingCallback(SimpleCallback);
        }

        public IExpressionEvalMapComponentsBuilder UsingEvaluationContext(IEvaluationContext context)
        {
            EvaluationContext = context;
            return _evalMapComponentsBuilder;
        }

        public IExpressionEvalMapComponentsBuilder WithRoot(object root)
        {
            Root = root;
            RootExplicitlySet = true;
            return _evalMapComponentsBuilder;
        }

        public IExpressionEvalMapComponentsBuilder WithReturnType(Type returnType)
        {
            ReturnType = returnType;
            return _evalMapComponentsBuilder;
        }

        private class ExpressionEvalMapFinalBuilderImpl : IExpressionEvalMapFinalBuilder
        {
            public ExpressionEvalMapFinalBuilderImpl(ExpressionEvalDictionaryBuilder builder)
            {
                Builder = builder;
            }

            protected ExpressionEvalDictionaryBuilder Builder { get; }

            public ExpressionEvalDictionary Build()
            {
                if (Builder.EvaluationCallback != null)
                {
                    return new ExpressionEvalDictionary(Builder.Expressions, Builder.EvaluationCallback);
                }
                else
                {
                    return new ExpressionEvalDictionary(
                        Builder.Expressions,
                        new ComponentsEvaluationCallback(Builder.EvaluationContext, Builder.Root, Builder.RootExplicitlySet, Builder.ReturnType));
                }
            }
        }

        private sealed class ExpressionEvalMapComponentsBuilderImpl : ExpressionEvalMapFinalBuilderImpl, IExpressionEvalMapComponentsBuilder
        {
            public ExpressionEvalMapComponentsBuilderImpl(ExpressionEvalDictionaryBuilder builder)
                : base(builder)
            {
            }

            public IExpressionEvalMapComponentsBuilder UsingEvaluationContext(IEvaluationContext context)
            {
                return Builder.UsingEvaluationContext(context);
            }

            public IExpressionEvalMapComponentsBuilder WithRoot(object root)
            {
                return Builder.WithRoot(root);
            }

            public IExpressionEvalMapComponentsBuilder WithReturnType(Type returnType)
            {
                return Builder.WithReturnType(returnType);
            }
        }
    }
}
