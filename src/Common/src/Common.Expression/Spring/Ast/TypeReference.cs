// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class TypeReference : SpelNode
    {
        private readonly int _dimensions;
        private Type _type;

        public TypeReference(int startPos, int endPos, SpelNode qualifiedId)
        : this(startPos, endPos, qualifiedId, 0)
        {
        }

        public TypeReference(int startPos, int endPos, SpelNode qualifiedId, int dims)
        : base(startPos, endPos, qualifiedId)
        {
            _dimensions = dims;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            // TODO possible optimization here if we cache the discovered type reference, but can we do that?
            var typeName = (string)_children[0].GetValueInternal(state).Value;
            if (typeName == null)
            {
                throw new InvalidOperationException("No type name");
            }

            if (!typeName.Contains(".") && char.IsLower(typeName[0]))
            {
                var tc = SpelTypeCode.ForName(typeName.ToUpper());
                if (tc != SpelTypeCode.OBJECT)
                {
                    // It is a primitive type
                    var atype = MakeArrayIfNecessary(tc.Type);
                    _exitTypeDescriptor = "LSystem/Type";
                    _type = atype;
                    return new TypedValue(atype);
                }
            }

            var clazz = state.FindType(typeName);
            clazz = MakeArrayIfNecessary(clazz);
            _exitTypeDescriptor = "LSystem/Type";
            _type = clazz;
            return new TypedValue(clazz);
        }

        public override string ToStringAST()
        {
            var sb = new StringBuilder("T(");
            sb.Append(GetChild(0).ToStringAST());
            for (var d = 0; d < _dimensions; d++)
            {
                sb.Append("[]");
            }

            sb.Append(")");
            return sb.ToString();
        }

        public override bool IsCompilable()
        {
            return _exitTypeDescriptor != null;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // // TODO Future optimization - if followed by a static method call, skip generating code here
            //    Assert.state(this.type != null, "No type available");
            //    if (this.type.isPrimitive())
            //    {
            //        if (this.type == Boolean.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Boolean", "TYPE", "Ljava/lang/Class;");
            //        }
            //        else if (this.type == Byte.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Byte", "TYPE", "Ljava/lang/Class;");
            //        }
            //        else if (this.type == Character.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Character", "TYPE", "Ljava/lang/Class;");
            //        }
            //        else if (this.type == Double.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Double", "TYPE", "Ljava/lang/Class;");
            //        }
            //        else if (this.type == Float.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Float", "TYPE", "Ljava/lang/Class;");
            //        }
            //        else if (this.type == Integer.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Integer", "TYPE", "Ljava/lang/Class;");
            //        }
            //        else if (this.type == Long.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Long", "TYPE", "Ljava/lang/Class;");
            //        }
            //        else if (this.type == Short.TYPE)
            //        {
            //            mv.visitFieldInsn(GETSTATIC, "java/lang/Short", "TYPE", "Ljava/lang/Class;");
            //        }
            //    }
            //    else
            //    {
            //        mv.visitLdcInsn(Type.getType(this.type));
            //    }
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }

        private Type MakeArrayIfNecessary(Type clazz)
        {
            if (_dimensions != 0)
            {
                for (var i = 0; i < _dimensions; i++)
                {
                    var array = Array.CreateInstance(clazz, 0);
                    clazz = array.GetType();
                }
            }

            return clazz;
        }
    }
}
