// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Steeltoe.Common.Converter;

public class ObjectToObjectConverter : AbstractGenericConditionalConverter
{
    private static readonly ConcurrentDictionary<Type, MemberInfo> ConversionMemberCache = new();

    public ObjectToObjectConverter()
        : base(GetConvertiblePairs())
    {
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        return sourceType != targetType && HasConversionMethodOrConstructor(targetType, sourceType);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        Type sourceClass = sourceType;
        Type targetClass = targetType;
        MemberInfo member = GetValidatedMember(targetClass, sourceClass);

        try
        {
            if (member is MethodInfo method)
            {
                return !method.IsStatic
                    ? method.Invoke(source, Array.Empty<object>())
                    : method.Invoke(null, new[]
                    {
                        source
                    });
            }

            if (member is ConstructorInfo ctor)
            {
                return ctor.Invoke(new[]
                {
                    source
                });
            }
        }
        catch (TargetInvocationException ex)
        {
            throw new ConversionFailedException(sourceType, targetType, source, ex.InnerException);
        }
        catch (Exception ex)
        {
            throw new ConversionFailedException(sourceType, targetType, source, ex);
        }

        throw new InvalidOperationException(string.Format(
            "No To{1}() method exists on {0}, " + "and no static ValueOf/Of/From({0}) method or {1}({0}) constructor exists on {1}.", sourceClass.Name,
            targetClass.Name));
    }

    internal static bool HasConversionMethodOrConstructor(Type targetType, Type sourceType)
    {
        return GetValidatedMember(targetType, sourceType) != null;
    }

    private static ISet<(Type SourceType, Type TargetType)> GetConvertiblePairs()
    {
        return new HashSet<(Type SourceType, Type TargetType)>
        {
            (typeof(object), typeof(object))
        };
    }

    private static MemberInfo GetValidatedMember(Type targetType, Type sourceType)
    {
        if (ConversionMemberCache.TryGetValue(targetType, out MemberInfo member) && IsApplicable(member, sourceType))
        {
            return member;
        }

        member = DetermineToMethod(targetType, sourceType);

        if (member == null)
        {
            member = DetermineFactoryMethod(targetType, sourceType);

            if (member == null)
            {
                member = DetermineFactoryConstructor(targetType, sourceType);

                if (member == null)
                {
                    return null;
                }
            }
        }

        ConversionMemberCache.TryAdd(targetType, member);
        return member;
    }

    private static bool IsApplicable(MemberInfo member, Type sourceType)
    {
        return member switch
        {
            MethodInfo method => !method.IsStatic ? method.DeclaringType.IsAssignableFrom(sourceType) : method.GetParameters()[0].ParameterType == sourceType,
            ConstructorInfo ctor => ctor.GetParameters()[0].ParameterType == sourceType,
            _ => false
        };
    }

    private static MethodInfo DetermineToMethod(Type targetType, Type sourceType)
    {
        if (typeof(string) == targetType || typeof(string) == sourceType)
        {
            // Do not accept a ToString() method or any to methods on String itself
            return null;
        }

        MethodInfo method = ConversionUtils.GetMethodIfAvailable(sourceType, $"To{targetType.Name}");
        return method != null && !method.IsStatic && targetType.IsAssignableFrom(method.ReturnType) ? method : null;
    }

    private static MethodInfo DetermineFactoryMethod(Type targetType, Type sourceType)
    {
        if (typeof(string) == targetType)
        {
            // Do not accept the String.valueOf(Object) method
            return null;
        }

        MethodInfo method = ConversionUtils.GetStaticMethod(targetType, "ValueOf", sourceType);

        if (method == null)
        {
            method = ConversionUtils.GetStaticMethod(targetType, "Of", sourceType);

            if (method == null)
            {
                method = ConversionUtils.GetStaticMethod(targetType, "From", sourceType);
            }
        }

        return method;
    }

    private static ConstructorInfo DetermineFactoryConstructor(Type targetType, Type sourceType)
    {
        return ConversionUtils.GetConstructorIfAvailable(targetType, sourceType);
    }
}
