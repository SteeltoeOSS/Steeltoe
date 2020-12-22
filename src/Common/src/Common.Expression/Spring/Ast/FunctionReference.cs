// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Support;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class FunctionReference : SpelNode
    {
        private readonly string _name;

        // Captures the most recently used method for the function invocation *if* the method
        // can safely be used for compilation (i.e. no argument conversion is going on)
        private volatile MethodInfo _method;

        public FunctionReference(string functionName, int startPos, int endPos, params SpelNode[] arguments)
        : base(startPos, endPos, arguments)
        {
            _name = functionName;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var value = state.LookupVariable(_name);
            if (value == TypedValue.NULL)
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.FUNCTION_NOT_DEFINED, _name);
            }

            if (!(value.Value is MethodInfo))
            {
                // Possibly a static Java method registered as a function
                throw new SpelEvaluationException(SpelMessage.FUNCTION_REFERENCE_CANNOT_BE_INVOKED, _name, value.GetType());
            }

            try
            {
                return ExecuteFunctionJLRMethod(state, (MethodInfo)value.Value);
            }
            catch (SpelEvaluationException ex)
            {
                ex.Position = StartPosition;
                throw;
            }
        }

        public override string ToStringAST()
        {
            var items = new List<string>();
            for (var i = 0; i < ChildCount; i++)
            {
                items.Add(GetChild(i).ToStringAST());
            }

            return '#' + _name + "(" + string.Join(",", items) + ")";
        }

        public override bool IsCompilable()
        {
            if (_method == null)
            {
                return false;
            }

            if (!_method.IsStatic || !_method.IsPublic || !_method.DeclaringType.IsPublic)
            {
                return false;
            }

            foreach (var child in _children)
            {
                if (!child.IsCompilable())
                {
                    return false;
                }
            }

            return true;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // Method method = this.method;
            // Assert.state(method != null, "No method handle");
            // String classDesc = method.getDeclaringClass().getName().replace('.', '/');
            // generateCodeForArguments(mv, cf, method, this.children);
            // mv.visitMethodInsn(INVOKESTATIC, classDesc, method.getName(),
            //        CodeFlow.createSignatureDescriptor(method), false);
            // cf.pushDescriptor(this.exitTypeDescriptor);
        }

        private object[] GetArguments(ExpressionState state)
        {
            // Compute arguments to the function
            var arguments = new object[ChildCount];
            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = _children[i].GetValueInternal(state).Value;
            }

            return arguments;
        }

        private TypedValue ExecuteFunctionJLRMethod(ExpressionState state, MethodInfo method)
        {
            var functionArgs = GetArguments(state);

            if (!method.IsVarArgs())
            {
                var declaredParamCount = method.GetParameters().Length;
                if (declaredParamCount != functionArgs.Length)
                {
                    throw new SpelEvaluationException(SpelMessage.INCORRECT_NUMBER_OF_ARGUMENTS_TO_FUNCTION, functionArgs.Length, declaredParamCount);
                }
            }

            if (!method.IsStatic)
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.FUNCTION_MUST_BE_STATIC, ClassUtils.GetQualifiedMethodName(method), _name);
            }

            // Convert arguments if necessary and remap them for varargs if required
            var converter = state.EvaluationContext.TypeConverter;
            var argumentConversionOccurred = ReflectionHelper.ConvertAllArguments(converter, functionArgs, method);
            if (method.IsVarArgs())
            {
                functionArgs = ReflectionHelper.SetupArgumentsForVarargsInvocation(ClassUtils.GetParameterTypes(method), functionArgs);
            }

            var compilable = false;
            try
            {
                var result = method.Invoke(method.GetType(), functionArgs);
                compilable = !argumentConversionOccurred;
                return new TypedValue(result, result == null ? method.ReturnType : result.GetType());
            }
            catch (Exception ex)
            {
                throw new SpelEvaluationException(StartPosition, ex, SpelMessage.EXCEPTION_DURING_FUNCTION_CALL, _name, ex.Message);
            }
            finally
            {
                if (compilable)
                {
                    _exitTypeDescriptor = CodeFlow.ToDescriptor(method.ReturnType);
                    _method = method;
                }
                else
                {
                    _exitTypeDescriptor = null;
                    _method = null;
                }
            }
        }
    }
}
