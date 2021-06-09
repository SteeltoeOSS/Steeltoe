// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class InlineList : SpelNode
    {
        private static readonly FieldInfo _fieldInfo = typeof(CompiledExpression).GetField("_dynamicFields", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo _getItemMethod = typeof(Dictionary<string, object>).GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo _addMethod = typeof(IList).GetMethod("Add", new Type[] { typeof(object) });
        private static readonly ConstructorInfo _listConstr = typeof(List<object>).GetConstructor(new Type[0]);

        // If the list is purely literals, it is a constant value and can be computed and cached
        private ITypedValue _constant;  // TODO must be immutable list

        public InlineList(int startPos, int endPos, params SpelNode[] args)
         : base(startPos, endPos, args)
        {
            CheckIfConstant();
        }

        public override ITypedValue GetValueInternal(ExpressionState expressionState)
        {
            if (_constant != null)
            {
                return _constant;
            }
            else
            {
                var childCount = ChildCount;
                var returnValue = new List<object>(childCount);
                for (var c = 0; c < childCount; c++)
                {
                    returnValue.Add(GetChild(c).GetValue(expressionState));
                }

                return new TypedValue(returnValue);
            }
        }

        public override string ToStringAST()
        {
            // String ast matches input string, not the 'toString()' of the resultant collection, which would use []
            var sj = new List<string>();
            var count = ChildCount;
            for (var c = 0; c < count; c++)
            {
                sj.Add(GetChild(c).ToStringAST());
            }

            return "{" + string.Join(",", sj) + "}";
        }

        public bool IsConstant => _constant != null;

        public IList<object> GetConstantValue()
        {
            if (_constant == null)
            {
                throw new InvalidOperationException("No constant");
            }

            return (IList<object>)_constant.Value;
        }

        public override bool IsCompilable() => IsConstant;

        public override void GenerateCode(ILGenerator gen, CodeFlow codeflow)
        {
            var constantFieldName = "inlineList$" + codeflow.NextFieldId();
            codeflow.RegisterNewField(constantFieldName, new List<object>());
            codeflow.RegisterNewInitGenerator((initGenerator, cflow) => { GenerateInitCode(constantFieldName, initGenerator, cflow); });

            GenerateLoadListCode(gen, constantFieldName);
            codeflow.PushDescriptor(new TypeDescriptor(typeof(IList)));
        }

        public void GenerateInitCode(string constantFieldName, ILGenerator gen, CodeFlow codeflow, bool nested = false)
        {
            LocalBuilder listLocal = null;
            if (!nested)
            {
                // Get list on stack
                GenerateLoadListCode(gen, constantFieldName);

                // Save to local for easy access
                listLocal = gen.DeclareLocal(typeof(IList));
                gen.Emit(OpCodes.Stloc, listLocal);
            }
            else
            {
                // Create nested list to work with
                gen.Emit(OpCodes.Newobj, _listConstr);
                gen.Emit(OpCodes.Castclass, typeof(IList));
            }

            var childCount = ChildCount;
            for (var c = 0; c < childCount; c++)
            {
                if (!nested)
                {
                    gen.Emit(OpCodes.Ldloc, listLocal);
                }
                else
                {
                    gen.Emit(OpCodes.Dup);
                }

                // The children might be further lists if they are not constants. In this
                // situation do not call back into generateCode() because it will register another clinit adder.
                // Instead, directly build the list here:
                if (_children[c] is InlineList)
                {
                    ((InlineList)_children[c]).GenerateInitCode(constantFieldName, gen, codeflow, true);
                }
                else
                {
                    _children[c].GenerateCode(gen, codeflow);
                    var lastDesc = codeflow.LastDescriptor();
                    if (CodeFlow.IsValueType(lastDesc))
                    {
                        CodeFlow.InsertBoxIfNecessary(gen, lastDesc);
                    }
                }

                gen.Emit(OpCodes.Callvirt, _addMethod);

                // Ignore int return
                gen.Emit(OpCodes.Pop);
            }
        }

        private void GenerateLoadListCode(ILGenerator gen, string constantFieldName)
        {
            // Load SpelCompiledExpression
            gen.Emit(OpCodes.Ldarg_0);

            // Get Dictionary<string, object> from CompiledExpression
            gen.Emit(OpCodes.Ldfld, _fieldInfo);

            // Get registered Field out of dictionary
            gen.Emit(OpCodes.Ldstr, constantFieldName);
            gen.Emit(OpCodes.Callvirt, _getItemMethod);
        }

        private void CheckIfConstant()
        {
            var isConstant = true;
            for (int c = 0, max = ChildCount; c < max; c++)
            {
                var child = GetChild(c);
                if (!(child is Literal))
                {
                    if (child is InlineList)
                    {
                        var inlineList = (InlineList)child;
                        if (!inlineList.IsConstant)
                        {
                            isConstant = false;
                        }
                    }
                    else
                    {
                        isConstant = false;
                    }
                }
            }

            if (isConstant)
            {
                var constantList = new List<object>();
                var childcount = ChildCount;
                for (var c = 0; c < childcount; c++)
                {
                    var child = GetChild(c);
                    if (child is Literal)
                    {
                        constantList.Add(((Literal)child).GetLiteralValue().Value);
                    }
                    else if (child is InlineList)
                    {
                        constantList.Add(((InlineList)child).GetConstantValue());
                    }
                }

                _constant = new TypedValue(constantList.AsReadOnly());
            }
        }
    }
}
