// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class ReflectiveConstructorResolver : IConstructorResolver
{
    public IConstructorExecutor Resolve(IEvaluationContext context, string typeName, List<Type> argumentTypes)
    {
        try
        {
            ITypeConverter typeConverter = context.TypeConverter;
            Type type = context.TypeLocator.FindType(typeName);

            if (IsPrimitive(type))
            {
                return new PrimitiveConstructorExecutor(type);
            }

            ConstructorInfo[] constructors = type.GetConstructors();

            Array.Sort(constructors, (c1, c2) =>
            {
                int c1Pl = c1.GetParameters().Length;
                int c2Pl = c2.GetParameters().Length;
                return c1Pl.CompareTo(c2Pl);
            });

            ConstructorInfo closeMatch = null;
            ConstructorInfo matchRequiringConversion = null;

            foreach (ConstructorInfo ctor in constructors)
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                int paramCount = parameters.Length;
                var paramDescriptors = new List<Type>(paramCount);

                for (int i = 0; i < paramCount; i++)
                {
                    paramDescriptors.Add(parameters[i].ParameterType);
                }

                ArgumentsMatchInfo matchInfo = null;

                if (ctor.IsVarArgs() && argumentTypes.Count >= paramCount - 1)
                {
                    // *sigh* complicated
                    // Basically.. we have to have all parameters match up until the varargs one, then the rest of what is
                    // being provided should be
                    // the same type whilst the final argument to the method must be an array of that (oh, how easy...not) -
                    // or the final parameter
                    // we are supplied does match exactly (it is an array already).
                    matchInfo = ReflectionHelper.CompareArgumentsVarargs(paramDescriptors, argumentTypes, typeConverter);
                }
                else if (paramCount == argumentTypes.Count)
                {
                    // worth a closer look
                    matchInfo = ReflectionHelper.CompareArguments(paramDescriptors, argumentTypes, typeConverter);
                }

                if (matchInfo != null)
                {
                    if (matchInfo.IsExactMatch)
                    {
                        return new ReflectiveConstructorExecutor(ctor);
                    }

                    if (matchInfo.IsCloseMatch)
                    {
                        closeMatch = ctor;
                    }
                    else if (matchInfo.IsMatchRequiringConversion)
                    {
                        matchRequiringConversion = ctor;
                    }
                }
            }

            if (closeMatch != null)
            {
                return new ReflectiveConstructorExecutor(closeMatch);
            }

            if (matchRequiringConversion != null)
            {
                return new ReflectiveConstructorExecutor(matchRequiringConversion);
            }

            return null;
        }
        catch (EvaluationException ex)
        {
            throw new AccessException("Failed to resolve constructor", ex);
        }
    }

    private bool IsPrimitive(Type type)
    {
        if (type.IsPrimitive || type == typeof(decimal))
        {
            return true;
        }

        return false;
    }
}
