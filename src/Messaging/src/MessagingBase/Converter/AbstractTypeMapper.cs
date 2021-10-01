// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Converter
{
    public abstract class AbstractTypeMapper
    {
        public const string DEFAULT_CLASSID_FIELD_NAME = MessageHeaders.TYPE_ID;
        public const string DEFAULT_CONTENT_CLASSID_FIELD_NAME = MessageHeaders.CONTENT_TYPE_ID;
        public const string DEFAULT_KEY_CLASSID_FIELD_NAME = MessageHeaders.KEY_TYPE_ID;

        private readonly Dictionary<string, Type> _idClassMapping = new ();

        private readonly Dictionary<Type, string> _classIdMapping = new ();

        public Dictionary<string, Type> IdClassMapping => _idClassMapping;

        public string ClassIdFieldName { get; internal set; } = DEFAULT_CLASSID_FIELD_NAME;

        public string ContentClassIdFieldName { get; internal set; } = DEFAULT_CONTENT_CLASSID_FIELD_NAME;

        public string KeyClassIdFieldName { get; internal set; } = DEFAULT_KEY_CLASSID_FIELD_NAME;

        public void SetIdClassMapping(Dictionary<string, Type> mapping)
        {
            foreach (var entry in mapping)
            {
                _idClassMapping[entry.Key] = entry.Value;
            }

            CreateReverseMap();
        }

        protected virtual void AddHeader(IMessageHeaders headers, string headerName, Type clazz)
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            if (_classIdMapping.ContainsKey(clazz))
            {
                accessor.SetHeader(headerName, _classIdMapping[clazz]);
            }
            else
            {
                accessor.SetHeader(headerName, GetClassName(clazz));
            }
        }

        protected virtual string RetrieveHeader(IMessageHeaders headers, string headerName)
        {
            var classId = RetrieveHeaderAsString(headers, headerName);
            if (classId == null)
            {
                throw new MessageConversionException(
                        "failed to convert Message content. Could not resolve " + headerName + " in header");
            }

            return classId;
        }

        protected virtual string RetrieveHeaderAsString(IMessageHeaders headers, string headerName)
        {
            var classIdFieldNameValue = headers.Get<object>(headerName);
            string classId = null;
            if (classIdFieldNameValue != null)
            {
                classId = classIdFieldNameValue.ToString();
            }

            return classId;
        }

        protected virtual bool HasInferredTypeHeader(IMessageHeaders headers)
        {
            return FromInferredTypeHeader(headers) != null;
        }

        protected Type FromInferredTypeHeader(IMessageHeaders headers)
        {
            return headers.Get<Type>(MessageHeaders.INFERRED_ARGUMENT_TYPE);
        }

        protected virtual Type GetContentType(Type type)
        {
            if (IsContainerType(type))
            {
                var typedef = type.GetGenericTypeDefinition();
                if (typeof(Dictionary<,>) == typedef)
                {
                    return type.GetGenericArguments()[1];
                }
                else
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        protected virtual bool IsContainerType(Type type)
        {
            if (type.IsGenericType)
            {
                var typedef = type.GetGenericTypeDefinition();
                if (typeof(Dictionary<,>) == typedef
                    || typeof(List<>) == typedef
                    || typeof(HashSet<>) == typedef
                    || typeof(LinkedList<>) == typedef
                    || typeof(Stack<>) == typedef
                    || typeof(Queue<>) == typedef)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual Type GetKeyType(Type type)
        {
            if (IsContainerType(type))
            {
                var typedef = type.GetGenericTypeDefinition();
                if (typeof(Dictionary<,>) == typedef)
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        protected virtual string GetClassName(Type type)
        {
            if (IsContainerType(type))
            {
                return type.GetGenericTypeDefinition().FullName;
            }

            return type.ToString();
        }

        private void CreateReverseMap()
        {
            _classIdMapping.Clear();
            foreach (var entry in _idClassMapping)
            {
                var id = entry.Key;
                var clazz = entry.Value;
                _classIdMapping[clazz] = id;
            }
        }
    }
}
