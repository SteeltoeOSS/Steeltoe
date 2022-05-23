// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class Elvis : SpelNode
    {
        private static readonly MethodInfo _equalsMethod = typeof(object).GetMethod("Equals", new[] { typeof(object) });

        public Elvis(int startPos, int endPos, params SpelNode[] args)
        : base(startPos, endPos, args)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var value = _children[0].GetValueInternal(state);

            // If this check is changed, the generateCode method will need changing too
            if (!(value.Value == null || string.Empty.Equals(value.Value)))
            {
                return value;
            }
            else
            {
                var result = _children[1].GetValueInternal(state);
                ComputeExitTypeDescriptor();
                return result;
            }
        }

        public override string ToStringAST()
        {
            return $"{GetChild(0).ToStringAST()} ?: {GetChild(1).ToStringAST()}";
        }

        public override bool IsCompilable()
        {
            var condition = _children[0];
            var ifNullValue = _children[1];
            return condition.IsCompilable() && ifNullValue.IsCompilable() &&
                    condition.ExitDescriptor != null && ifNullValue.ExitDescriptor != null;
        }

        public override void GenerateCode(ILGenerator gen, CodeFlow cf)
        {
            // exit type descriptor can be null if both components are literal expressions
            ComputeExitTypeDescriptor();
            cf.EnterCompilationScope();

            _children[0].GenerateCode(gen, cf);
            var lastDesc = cf.LastDescriptor();
            if (lastDesc == null)
            {
                throw new InvalidOperationException("No last descriptor");
            }

            // if primitive result, boxed will be on stack
            CodeFlow.InsertBoxIfNecessary(gen, lastDesc);
            cf.ExitCompilationScope();

            var ifResult = gen.DeclareLocal(typeof(bool));
            var finalResult = gen.DeclareLocal(_exitTypeDescriptor.Value);
            var loadFinalResult = gen.DefineLabel();

            // Save off child1 result
            var child1Result = gen.DeclareLocal(typeof(object));
            gen.Emit(OpCodes.Stloc, child1Result);

            var child1IsNull = gen.DefineLabel();
            gen.Emit(OpCodes.Ldloc, child1Result);

            // br if child1 null
            gen.Emit(OpCodes.Brfalse, child1IsNull);

            // Check for empty string
            gen.Emit(OpCodes.Ldstr, string.Empty);
            gen.Emit(OpCodes.Ldloc, child1Result);
            gen.Emit(OpCodes.Callvirt, _equalsMethod);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ceq);

            // save empty string result
            gen.Emit(OpCodes.Stloc, ifResult);
            var loadCheckIfResults = gen.DefineLabel();
            gen.Emit(OpCodes.Br, loadCheckIfResults);

            // Child1 null, load false for if result
            gen.MarkLabel(child1IsNull);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Stloc, ifResult);

            // Fall thru to check if results;
            // Mark Check if Results
            gen.MarkLabel(loadCheckIfResults);

            // Load if results;
            gen.Emit(OpCodes.Ldloc, ifResult);
            var callChild2 = gen.DefineLabel();

            // If faild, call child2 for results
            gen.Emit(OpCodes.Brfalse, callChild2);

            // Final result is child 1, save final
            gen.Emit(OpCodes.Ldloc, child1Result);
            gen.Emit(OpCodes.Stloc, finalResult);
            gen.Emit(OpCodes.Br, loadFinalResult);

            gen.MarkLabel(callChild2);
            cf.EnterCompilationScope();
            _children[1].GenerateCode(gen, cf);

            if (!CodeFlow.IsValueType(_exitTypeDescriptor))
            {
                lastDesc = cf.LastDescriptor();
                if (lastDesc == null)
                {
                    throw new InvalidOperationException("No last descriptor");
                }

                if (lastDesc == TypeDescriptor.V)
                {
                    gen.Emit(OpCodes.Ldnull);
                }
                else
                {
                    CodeFlow.InsertBoxIfNecessary(gen, lastDesc);
                }
            }

            cf.ExitCompilationScope();
            gen.Emit(OpCodes.Stloc, finalResult);

            // Load final result on stack
            gen.MarkLabel(loadFinalResult);
            gen.Emit(OpCodes.Ldloc, finalResult);
            cf.PushDescriptor(_exitTypeDescriptor);
        }

        private void ComputeExitTypeDescriptor()
        {
            if (_exitTypeDescriptor == null && _children[0].ExitDescriptor != null && _children[1].ExitDescriptor != null)
            {
                var conditionDescriptor = _children[0].ExitDescriptor;
                var ifNullValueDescriptor = _children[1].ExitDescriptor;
                if (ObjectUtils.NullSafeEquals(conditionDescriptor, ifNullValueDescriptor))
                {
                    _exitTypeDescriptor = conditionDescriptor;
                }
                else
                {
                    // Use the easiest to compute common super type
                    _exitTypeDescriptor = TypeDescriptor.OBJECT;
                }
            }
        }
    }
}
