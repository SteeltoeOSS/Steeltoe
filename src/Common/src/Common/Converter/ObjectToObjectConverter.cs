// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Common.Converter
{
    public class ObjectToObjectConverter : AbstractGenericConditionalConverter
    {
        private static readonly ConcurrentDictionary<Type, MemberInfo> _conversionMemberCache = new ConcurrentDictionary<Type, MemberInfo>();

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

            var sourceClass = sourceType;
            var targetClass = targetType;
            var member = GetValidatedMember(targetClass, sourceClass);

            try
            {
                if (member is MethodInfo)
                {
                    var method = (MethodInfo)member;
                    if (!method.IsStatic)
                    {
                        return method.Invoke(source, new object[0]);
                    }
                    else
                    {
                        return method.Invoke(null, new object[1] { source });
                    }
                }
                else if (member is ConstructorInfo)
                {
                    var ctor = (ConstructorInfo)member;
                    return ctor.Invoke(new object[1] { source });
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
                "No To{1}() method exists on {0}, " +
                    "and no static ValueOf/Of/From({0}) method or {1}({0}) constructor exists on {1}.",
                sourceClass.Name,
                targetClass.Name));
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>()
            {
                (typeof(object), typeof(object))
            };
        }

        private static bool HasConversionMethodOrConstructor(Type targetClass, Type sourceClass)
        {
            return GetValidatedMember(targetClass, sourceClass) != null;
        }

        private static MemberInfo GetValidatedMember(Type targetClass, Type sourceClass)
        {
            if (_conversionMemberCache.TryGetValue(targetClass, out var member) && IsApplicable(member, sourceClass))
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

            _conversionMemberCache.TryAdd(targetClass, member);
            return member;
        }

        private static bool IsApplicable(MemberInfo member, Type sourceClass)
        {
            if (member is MethodInfo)
            {
                var method = (MethodInfo)member;
                return !method.IsStatic ?
                    method.DeclaringType.IsAssignableFrom(sourceClass) :
                        method.GetParameters()[0].ParameterType == sourceClass;
            }
            else if (member is ConstructorInfo)
            {
                var ctor = (ConstructorInfo)member;
                return ctor.GetParameters()[0].ParameterType == sourceClass;
            }
            else
            {
                return false;
            }
        }

        private static MethodInfo DetermineToMethod(Type targetClass, Type sourceClass)
        {
            if (typeof(string) == targetClass || typeof(string) == sourceClass)
            {
                // Do not accept a ToString() method or any to methods on String itself
                return null;
            }

            var method = ConversionUtils.GetMethodIfAvailable(sourceClass, "To" + targetClass.Name);
            return method != null && !method.IsStatic && targetClass.IsAssignableFrom(method.ReturnType) ? method : null;
        }

        private static MethodInfo DetermineFactoryMethod(Type targetClass, Type sourceClass)
        {
            if (typeof(string) == targetClass)
            {
                // Do not accept the String.valueOf(Object) method
                return null;
            }

            var method = ConversionUtils.GetStaticMethod(targetClass, "ValueOf", sourceClass);
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
}
