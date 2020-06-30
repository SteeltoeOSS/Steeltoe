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

using Steeltoe.Messaging.Converter;
using System;
using System.Collections.Generic;
using System.Runtime.Loader;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class DefaultTypeMapper : AbstractTypeMapper, ITypeMapper
    {
        public TypePrecedence Precedence { get; set; } = TypePrecedence.INFERRED;

        public Type DefaultType { get; set; } = typeof(object);

        public Type ToType(IMessageHeaders headers)
        {
            var inferredType = GetInferredType(headers);
            if (inferredType != null && (!inferredType.IsAbstract && !inferredType.IsInterface))
            {
                return inferredType;
            }

            var typeIdHeader = RetrieveHeaderAsString(headers, ClassIdFieldName);

            if (typeIdHeader != null)
            {
                return FromTypeHeader(headers, typeIdHeader);
            }

            if (HasInferredTypeHeader(headers))
            {
                return FromInferredTypeHeader(headers);
            }

            return DefaultType;
        }

        public void FromType(Type type, IMessageHeaders headers)
        {
            AddHeader(headers, ClassIdFieldName, type);

            if (IsContainerType(type))
            {
                AddHeader(headers, ContentClassIdFieldName, GetContentType(type));
            }

            Type keyType = GetKeyType(type);
            if (keyType != null)
            {
                AddHeader(headers, KeyClassIdFieldName, keyType);
            }
        }

        public Type GetInferredType(IMessageHeaders headers)
        {
            if (HasInferredTypeHeader(headers) && Precedence.Equals(TypePrecedence.INFERRED))
            {
                return FromInferredTypeHeader(headers);
            }

            return null;
        }

        private Type FromTypeHeader(IMessageHeaders headers, string typeIdHeader)
        {
            var classType = GetClassIdType(typeIdHeader);
            if (!IsContainerType(classType) || classType.IsArray)
            {
                return classType;
            }

            var contentClassType = GetClassIdType(RetrieveHeader(headers, ContentClassIdFieldName));
            if (!HasKeyType(classType))
            {
                return ConstructCollectionType(classType, contentClassType);
            }

            var keyClassType = GetClassIdType(RetrieveHeader(headers, KeyClassIdFieldName));
            return ConstructDictionaryType(classType, keyClassType, contentClassType);
        }

        private bool HasKeyType(Type classType)
        {
            if (typeof(Dictionary<,>) == classType)
            {
                return true;
            }

            return false;
        }

        private Type ConstructDictionaryType(Type classType, Type keyClassType, Type contentClassType)
        {
            return classType.MakeGenericType(keyClassType, contentClassType);
        }

        private Type ConstructCollectionType(Type classType, Type contentClassType)
        {
            return classType.MakeGenericType(contentClassType);
        }

        private Type GetClassIdType(string classId)
        {
            if (IdClassMapping.ContainsKey(classId))
            {
                return IdClassMapping[classId];
            }
            else
            {
                try
                {
                    return Type.GetType(classId, true);
                }
                catch (Exception)
                {
                    foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
                    {
                        var result = assembly.GetType(classId, false);
                        if (result != null)
                        {
                            return result;
                        }
                    }

                    throw new MessageConversionException("failed to resolve class name. Class not found [" + classId + "]");
                }
            }
        }
    }
}
