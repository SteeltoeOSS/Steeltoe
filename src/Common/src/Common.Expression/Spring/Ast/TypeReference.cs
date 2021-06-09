// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class TypeReference : SpelNode
    {
        private static readonly MethodInfo _getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);
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
                    _exitTypeDescriptor = TypeDescriptor.TYPE;
                    _type = atype;
                    return new TypedValue(atype);
                }
            }

            var clazz = state.FindType(typeName);
            clazz = MakeArrayIfNecessary(clazz);
            _exitTypeDescriptor = TypeDescriptor.TYPE;
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

        public override void GenerateCode(ILGenerator gen, CodeFlow cf)
        {
            if (_type == null)
            {
                throw new InvalidOperationException("No type available");
            }

            gen.Emit(OpCodes.Ldtoken, _type);
            gen.Emit(OpCodes.Call, _getTypeFromHandle);
            cf.PushDescriptor(_exitTypeDescriptor);
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
