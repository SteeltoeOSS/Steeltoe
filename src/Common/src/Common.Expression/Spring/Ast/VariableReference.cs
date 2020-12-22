// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class VariableReference : SpelNode
    {
        // Well known variables:
        private static readonly string _THIS = "this";  // currently active context object
        private static readonly string _ROOT = "root";  // root context object

        private readonly string _name;

        public VariableReference(string variableName, int startPos, int endPos)
            : base(startPos, endPos)
        {
            _name = variableName;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            if (_name.Equals(_THIS))
            {
                return state.GetActiveContextObject();
            }

            if (_name.Equals(_ROOT))
            {
                var obj = state.RootContextObject;
                _exitTypeDescriptor = CodeFlow.ToDescriptorFromObject(obj.Value);
                return obj;
            }

            var result = state.LookupVariable(_name);
            var value = result.Value;
            if (value == null || !value.GetType().IsPublic)
            {
                // If the type is not public then when generateCode produces a checkcast to it
                // then an IllegalAccessError will occur.
                // If resorting to Object isn't sufficient, the hierarchy could be traversed for
                // the first public type.
                _exitTypeDescriptor = "Ljava/lang/Object";
            }
            else
            {
                _exitTypeDescriptor = CodeFlow.ToDescriptorFromObject(value);
            }

            // a null value will mean either the value was null or the variable was not found
            return result;
        }

        public override void SetValue(ExpressionState state, object value)
        {
            state.SetVariable(_name, value);
        }

        public override string ToStringAST()
        {
            return "#" + _name;
        }

        public override bool IsWritable(ExpressionState expressionState)
        {
            return !(_name.Equals(_THIS) || _name.Equals(_ROOT));
        }

        public override bool IsCompilable()
        {
            return _exitTypeDescriptor != null;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // if (this.name.equals(ROOT))
            //    {
            //        mv.visitVarInsn(ALOAD, 1);
            //    }
            //    else
            //    {
            //        mv.visitVarInsn(ALOAD, 2);
            //        mv.visitLdcInsn(this.name);
            //        mv.visitMethodInsn(INVOKEINTERFACE, "org/springframework/expression/EvaluationContext", "lookupVariable", "(Ljava/lang/String;)Ljava/lang/Object;", true);
            //    }
            //    CodeFlow.insertCheckCast(mv, this.exitTypeDescriptor);
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }

        protected internal override IValueRef GetValueRef(ExpressionState state)
        {
            if (_name.Equals(_THIS))
            {
                return new TypedValueHolderValueRef(state.GetActiveContextObject(), this);
            }

            if (_name.Equals(_ROOT))
            {
                return new TypedValueHolderValueRef(state.RootContextObject, this);
            }

            var result = state.LookupVariable(_name);

            // a null value will mean either the value was null or the variable was not found
            return new VariableRef(_name, result, state.EvaluationContext);
        }

        private class VariableRef : IValueRef
        {
            private readonly string _name;
            private readonly ITypedValue _value;
            private readonly IEvaluationContext _evaluationContext;

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

            public bool IsWritable => true;
        }
    }
}
