// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class VariableReference : SpelNode
{
    // Well known variables:
    private static readonly string This = "this"; // currently active context object
    private static readonly string Root = "root"; // root context object

    private readonly string _name;
    private MethodInfo _method;

    public VariableReference(string variableName, int startPos, int endPos)
        : base(startPos, endPos)
    {
        _name = variableName;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        if (_name.Equals(This))
        {
            return state.GetActiveContextObject();
        }

        if (_name.Equals(Root))
        {
            ITypedValue obj = state.RootContextObject;
            exitTypeDescriptor = CodeFlow.ToDescriptorFromObject(obj.Value);
            return obj;
        }

        ITypedValue result = state.LookupVariable(_name);
        object value = result.Value;

        if (value == null || !ReflectionHelper.IsPublic(value.GetType()))
        {
            // If the type is not public then when generateCode produces a checkcast to it
            // then an IllegalAccessError will occur.
            // If resorting to Object isn't sufficient, the hierarchy could be traversed for
            // the first public type.
            exitTypeDescriptor = TypeDescriptor.Object;
        }
        else
        {
            exitTypeDescriptor = CodeFlow.ToDescriptorFromObject(value);
        }

        // a null value will mean either the value was null or the variable was not found
        return result;
    }

    public override void SetValue(ExpressionState state, object newValue)
    {
        state.SetVariable(_name, newValue);
    }

    public override string ToStringAst()
    {
        return $"#{_name}";
    }

    public override bool IsWritable(ExpressionState state)
    {
        return !(_name.Equals(This) || _name.Equals(Root));
    }

    public override bool IsCompilable()
    {
        return exitTypeDescriptor != null;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        if (_name.Equals(Root))
        {
            CodeFlow.LoadTarget(gen);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldstr, _name);
            gen.Emit(OpCodes.Callvirt, GetLookUpVariableMethod());
        }

        CodeFlow.InsertCastClass(gen, exitTypeDescriptor);
        cf.PushDescriptor(exitTypeDescriptor);
    }

    protected internal override IValueRef GetValueRef(ExpressionState state)
    {
        if (_name.Equals(This))
        {
            return new TypedValueHolderValueRef(state.GetActiveContextObject(), this);
        }

        if (_name.Equals(Root))
        {
            return new TypedValueHolderValueRef(state.RootContextObject, this);
        }

        ITypedValue result = state.LookupVariable(_name);

        // a null value will mean either the value was null or the variable was not found
        return new VariableRef(_name, result, state.EvaluationContext);
    }

    private MethodInfo GetLookUpVariableMethod()
    {
        if (_method == null)
        {
            _method = typeof(IEvaluationContext).GetMethods().Single(m => m.Name == "LookupVariable" && !m.IsGenericMethod);
        }

        return _method;
    }

    private sealed class VariableRef : IValueRef
    {
        private readonly string _name;
        private readonly ITypedValue _value;
        private readonly IEvaluationContext _evaluationContext;

        public bool IsWritable => true;

        public VariableRef(string name, ITypedValue value, IEvaluationContext evaluationContext)
        {
            _name = name;
            _value = value;
            _evaluationContext = evaluationContext;
        }

        public ITypedValue GetValue()
        {
            return _value;
        }

        public void SetValue(object newValue)
        {
            _evaluationContext.SetVariable(_name, newValue);
        }
    }
}
