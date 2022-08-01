// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class PrimitiveConstructorExecutor : IConstructorExecutor
{
    private readonly Type _primitiveType;

    public PrimitiveConstructorExecutor(Type primitiveType)
    {
        _primitiveType = primitiveType;
    }

    public ITypedValue Execute(IEvaluationContext context, params object[] arguments)
    {
        if (arguments.Length != 1 || arguments[0] == null)
        {
            throw new AccessException($"Invalid argument for primitive type:{_primitiveType}");
        }

        var argType = arguments[0].GetType();
        if (argType != _primitiveType)
        {
            var value = context.TypeConverter.ConvertValue(arguments[0], argType, _primitiveType);
            return new TypedValue(value, _primitiveType);
        }

        return new TypedValue(arguments[0], _primitiveType);
    }
}
