// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class InlineList : SpelNode
    {
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

        public override void GenerateCode(DynamicMethod mv, CodeFlow codeflow)
        {
            // String constantFieldName = "inlineList$" + codeflow.nextFieldId();
            // String className = codeflow.getClassName();

            // codeflow.registerNewField((cw, cflow)->
            //        cw.visitField(ACC_PRIVATE | ACC_STATIC | ACC_FINAL, constantFieldName, "Ljava/util/List;", null, null));

            // codeflow.registerNewClinit((mVisitor, cflow)->
            //        generateClinitCode(className, constantFieldName, mVisitor, cflow, false));

            // mv.visitFieldInsn(GETSTATIC, className, constantFieldName, "Ljava/util/List;");
            // codeflow.pushDescriptor("Ljava/util/List");
        }

        // void GenerateClinitCode(string clazzname, string constantFieldName, DynamicMethod mv, CodeFlow codeflow, bool nested)
        // {
        //    mv.visitTypeInsn(NEW, "java/util/ArrayList");
        //    mv.visitInsn(DUP);
        //    mv.visitMethodInsn(INVOKESPECIAL, "java/util/ArrayList", "<init>", "()V", false);
        //    if (!nested)
        //    {
        //        mv.visitFieldInsn(PUTSTATIC, clazzname, constantFieldName, "Ljava/util/List;");
        //    }
        //    int childCount = getChildCount();
        //    for (var c = 0; c < childCount; c++)
        //    {
        //        if (!nested)
        //        {
        //            mv.visitFieldInsn(GETSTATIC, clazzname, constantFieldName, "Ljava/util/List;");
        //        }
        //        else
        //        {
        //            mv.visitInsn(DUP);
        //        }
        //        // The children might be further lists if they are not constants. In this
        //        // situation do not call back into generateCode() because it will register another clinit adder.
        //        // Instead, directly build the list here:
        //        if (this.children[c] instanceof InlineList) {
        //        ((InlineList)this.children[c]).generateClinitCode(clazzname, constantFieldName, mv, codeflow, true);
        //    }

        // else
        //    {
        //        this.children[c].generateCode(mv, codeflow);
        //        String lastDesc = codeflow.lastDescriptor();
        //        if (CodeFlow.isPrimitive(lastDesc))
        //        {
        //            CodeFlow.insertBoxIfNecessary(mv, lastDesc.charAt(0));
        //        }
        //    }
        //    mv.visitMethodInsn(INVOKEINTERFACE, "java/util/List", "add", "(Ljava/lang/Object;)Z", true);
        //    mv.visitInsn(POP);
        // }
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
