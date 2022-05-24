// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class ExpressionState
    {
        private Stack<ITypedValue> _contextObjects;

        private Stack<VariableScope> _variableScopes;

        private Stack<ITypedValue> _scopeRootObjects;

        public ExpressionState(IEvaluationContext context)
        : this(context, context.RootObject, new SpelParserOptions(false, false))
        {
        }

        public ExpressionState(IEvaluationContext context, SpelParserOptions configuration)
        : this(context, context.RootObject, configuration)
        {
        }

        public ExpressionState(IEvaluationContext context, ITypedValue rootObject)
        : this(context, rootObject, new SpelParserOptions(false, false))
        {
        }

        public ExpressionState(IEvaluationContext context, ITypedValue rootObject, SpelParserOptions configuration)
        {
            EvaluationContext = context ?? throw new ArgumentNullException(nameof(context));
            RootContextObject = rootObject;
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public List<IPropertyAccessor> PropertyAccessors => EvaluationContext.PropertyAccessors;

        public IEvaluationContext EvaluationContext { get; }

        public SpelParserOptions Configuration { get; }

        public ITypedValue RootContextObject { get; }

        public ITypeComparator TypeComparator => EvaluationContext.TypeComparator;

        public ITypeConverter TypeConverter => EvaluationContext.TypeConverter;

        public ITypedValue GetActiveContextObject()
        {
            if (_contextObjects == null || _contextObjects.Count == 0)
            {
                return RootContextObject;
            }

            return _contextObjects.Peek();
        }

        public void PushActiveContextObject(ITypedValue obj)
        {
            _contextObjects ??= new Stack<ITypedValue>();

            _contextObjects.Push(obj);
        }

        public void PopActiveContextObject()
        {
            _contextObjects ??= new Stack<ITypedValue>();

            _contextObjects.Pop();
        }

        public ITypedValue GetScopeRootContextObject()
        {
            if (_scopeRootObjects == null || _scopeRootObjects.Count == 0)
            {
                return RootContextObject;
            }

            return _scopeRootObjects.Peek();
        }

        public void SetVariable(string name, object value)
        {
            EvaluationContext.SetVariable(name, value);
        }

        public ITypedValue LookupVariable(string name)
        {
            var value = EvaluationContext.LookupVariable(name);
            return value != null ? new TypedValue(value) : TypedValue.NULL;
        }

        public Type FindType(string type)
        {
            return EvaluationContext.TypeLocator.FindType(type);
        }

        public object ConvertValue(object value, Type targetTypeDescriptor)
        {
            var result = EvaluationContext.TypeConverter.ConvertValue(value, value?.GetType(), targetTypeDescriptor);
            if (result == null)
            {
                throw new InvalidOperationException($"Null conversion result for value [{value}]");
            }

            return result;
        }

        public object ConvertValue(ITypedValue value, Type targetTypeDescriptor)
        {
            var val = value.Value;
            return EvaluationContext.TypeConverter.ConvertValue(val, val?.GetType(), targetTypeDescriptor);
        }

        public void EnterScope(Dictionary<string, object> argMap)
        {
            InitVariableScopes().Push(new VariableScope(argMap));
            InitScopeRootObjects().Push(GetActiveContextObject());
        }

        public void EnterScope()
        {
            InitVariableScopes().Push(new VariableScope(new Dictionary<string, object>()));
            InitScopeRootObjects().Push(GetActiveContextObject());
        }

        public void EnterScope(string name, object value)
        {
            InitVariableScopes().Push(new VariableScope(name, value));
            InitScopeRootObjects().Push(GetActiveContextObject());
        }

        public void ExitScope()
        {
            InitVariableScopes().Pop();
            InitScopeRootObjects().Pop();
        }

        public void SetLocalVariable(string name, object value)
        {
            InitVariableScopes().Peek().SetVariable(name, value);
        }

        public object LookupLocalVariable(string name)
        {
            foreach (var scope in InitVariableScopes())
            {
                if (scope.DefinesVariable(name))
                {
                    return scope.LookupVariable(name);
                }
            }

            return null;
        }

        public ITypedValue Operate(Operation op, object left, object right)
        {
            var overloader = EvaluationContext.OperatorOverloader;
            if (overloader.OverridesOperation(op, left, right))
            {
                var returnValue = overloader.Operate(op, left, right);
                return new TypedValue(returnValue);
            }
            else
            {
                var leftType = left == null ? "null" : left.GetType().FullName;
                var rightType = right == null ? "null" : right.GetType().FullName;
                throw new SpelEvaluationException(SpelMessage.OPERATOR_NOT_SUPPORTED_BETWEEN_TYPES, op, leftType, rightType);
            }
        }

        private Stack<VariableScope> InitVariableScopes()
        {
            if (_variableScopes == null)
            {
                _variableScopes = new Stack<VariableScope>();

                // top-level empty variable scope
                _variableScopes.Push(new VariableScope());
            }

            return _variableScopes;
        }

        private Stack<ITypedValue> InitScopeRootObjects()
        {
            _scopeRootObjects ??= new Stack<ITypedValue>();
            return _scopeRootObjects;
        }

        private sealed class VariableScope
        {
            private readonly Dictionary<string, object> _vars = new ();

            public VariableScope()
            {
            }

            public VariableScope(Dictionary<string, object> arguments)
            {
                if (arguments != null)
                {
                    foreach (var args in arguments)
                    {
                        _vars[args.Key] = args.Value;
                    }
                }
            }

            public VariableScope(string name, object value)
            {
                _vars[name] = value;
            }

            public object LookupVariable(string name)
            {
                _vars.TryGetValue(name, out var val);
                return val;
            }

            public void SetVariable(string name, object value)
            {
                _vars[name] = value;
            }

            public bool DefinesVariable(string name)
            {
                return _vars.ContainsKey(name);
            }
        }
    }
}
