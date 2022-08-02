// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class TypeReference : SpelNode
{
    private static readonly MethodInfo GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Static | BindingFlags.Public);
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
        // Possible optimization here if we cache the discovered type reference, but can we do that?
        string typeName = (string)children[0].GetValueInternal(state).Value;

        if (typeName == null)
        {
            throw new InvalidOperationException("No type name");
        }

        if (!typeName.Contains(".") && char.IsLower(typeName[0]))
        {
            SpelTypeCode tc = SpelTypeCode.ForName(typeName.ToUpper());

            if (tc != SpelTypeCode.Object)
            {
                // It is a primitive type
                Type type = MakeArrayIfNecessary(tc.Type);
                exitTypeDescriptor = TypeDescriptor.Type;
                _type = type;
                return new TypedValue(type);
            }
        }

        Type clazz = state.FindType(typeName);
        clazz = MakeArrayIfNecessary(clazz);
        exitTypeDescriptor = TypeDescriptor.Type;
        _type = clazz;
        return new TypedValue(clazz);
    }

    public override string ToStringAst()
    {
        var sb = new StringBuilder("T(");
        sb.Append(GetChild(0).ToStringAst());

        for (int d = 0; d < _dimensions; d++)
        {
            sb.Append("[]");
        }

        sb.Append(')');
        return sb.ToString();
    }

    public override bool IsCompilable()
    {
        return exitTypeDescriptor != null;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        if (_type == null)
        {
            throw new InvalidOperationException("No type available");
        }

        gen.Emit(OpCodes.Ldtoken, _type);
        gen.Emit(OpCodes.Call, GetTypeFromHandle);
        cf.PushDescriptor(exitTypeDescriptor);
    }

    private Type MakeArrayIfNecessary(Type clazz)
    {
        if (_dimensions != 0)
        {
            for (int i = 0; i < _dimensions; i++)
            {
                var array = Array.CreateInstance(clazz, 0);
                clazz = array.GetType();
            }
        }

        return clazz;
    }
}
