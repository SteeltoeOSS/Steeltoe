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

    internal static bool HasConversionMethodOrConstructor(Type targetClass, Type sourceClass)
    {
        return GetValidatedMember(targetClass, sourceClass) != null;
    }

    private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
    {
        return new HashSet<(Type Source, Type Target)>
        {
            (typeof(object), typeof(object))
        };
    }

    private static MemberInfo GetValidatedMember(Type targetClass, Type sourceClass)
    {
        if (ConversionMemberCache.TryGetValue(targetClass, out MemberInfo member) && IsApplicable(member, sourceClass))
        {
            return member;
        }

        member = DetermineToMethod(targetClass, sourceClass);

        if (member == null)
        {
            member = DetermineFactoryMethod(targetClass, sourceClass);

            if (member == null)
            {
                member = DetermineFactoryConstructor(targetClass, sourceClass);

                if (member == null)
                {
                    return null;
                }
            }
        }

        ConversionMemberCache.TryAdd(targetClass, member);
        return member;
    }

    private static bool IsApplicable(MemberInfo member, Type sourceClass)
    {
        return member switch
        {
            MethodInfo method => !method.IsStatic ? method.DeclaringType.IsAssignableFrom(sourceClass) : method.GetParameters()[0].ParameterType == sourceClass,
            ConstructorInfo ctor => ctor.GetParameters()[0].ParameterType == sourceClass,
            _ => false
        };
    }

    private static MethodInfo DetermineToMethod(Type targetClass, Type sourceClass)
    {
        if (typeof(string) == targetClass || typeof(string) == sourceClass)
        {
            // Do not accept a ToString() method or any to methods on String itself
            return null;
        }

        MethodInfo method = ConversionUtils.GetMethodIfAvailable(sourceClass, $"To{targetClass.Name}");
        return method != null && !method.IsStatic && targetClass.IsAssignableFrom(method.ReturnType) ? method : null;
    }

    private static MethodInfo DetermineFactoryMethod(Type targetClass, Type sourceClass)
    {
        if (typeof(string) == targetClass)
        {
            // Do not accept the String.valueOf(Object) method
            return null;
        }

        MethodInfo method = ConversionUtils.GetStaticMethod(targetClass, "ValueOf", sourceClass);

        if (method == null)
        {
            method = ConversionUtils.GetStaticMethod(targetClass, "Of", sourceClass);

            if (method == null)
            {
                method = ConversionUtils.GetStaticMethod(targetClass, "From", sourceClass);
            }
        }

        return method;
    }

    private static ConstructorInfo DetermineFactoryConstructor(Type targetClass, Type sourceClass)
    {
        return ConversionUtils.GetConstructorIfAvailable(targetClass, sourceClass);
    }
}
