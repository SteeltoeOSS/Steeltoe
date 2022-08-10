// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;

namespace Steeltoe.Messaging.Converter;

public abstract class AbstractTypeMapper
{
    public const string DefaultClassIdFieldName = MessageHeaders.TypeId;
    public const string DefaultContentClassIdFieldName = MessageHeaders.ContentTypeId;
    public const string DefaultKeyClassIdFieldName = MessageHeaders.KeyTypeId;

    private static readonly ISet<Type> ContainerTypes = new[]
    {
        typeof(Dictionary<,>),
        typeof(List<>),
        typeof(HashSet<>),
        typeof(LinkedList<>),
        typeof(Stack<>),
        typeof(Queue<>)
    }.ToHashSet();

    private readonly Dictionary<Type, string> _classIdMapping = new();

    public Dictionary<string, Type> IdClassMapping { get; } = new();

    public string ClassIdFieldName { get; internal set; } = DefaultClassIdFieldName;

    public string ContentClassIdFieldName { get; internal set; } = DefaultContentClassIdFieldName;

    public string KeyClassIdFieldName { get; internal set; } = DefaultKeyClassIdFieldName;

    public void SetIdClassMapping(Dictionary<string, Type> mapping)
    {
        foreach (KeyValuePair<string, Type> entry in mapping)
        {
            IdClassMapping[entry.Key] = entry.Value;
        }

        CreateReverseMap();
    }

    protected virtual void AddHeader(IMessageHeaders headers, string headerName, Type type)
    {
        MessageHeaderAccessor accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
        accessor.SetHeader(headerName, _classIdMapping.ContainsKey(type) ? _classIdMapping[type] : GetClassName(type));
    }

    protected virtual string RetrieveHeader(IMessageHeaders headers, string headerName)
    {
        string classId = RetrieveHeaderAsString(headers, headerName);

        if (classId == null)
        {
            throw new MessageConversionException($"failed to convert Message content. Could not resolve {headerName} in header");
        }

        return classId;
    }

    protected virtual string RetrieveHeaderAsString(IMessageHeaders headers, string headerName)
    {
        object classIdFieldNameValue = headers.Get<object>(headerName);
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
        return headers.Get<Type>(MessageHeaders.InferredArgumentType);
    }

    protected virtual Type GetContentType(Type type)
    {
        if (IsContainerType(type))
        {
            Type typedef = type.GetGenericTypeDefinition();

            if (typeof(Dictionary<,>) == typedef)
            {
                return type.GetGenericArguments()[1];
            }

            return type.GetGenericArguments()[0];
        }

        return null;
    }

    protected virtual bool IsContainerType(Type type)
    {
        if (type.IsGenericType)
        {
            Type genericType = type.GetGenericTypeDefinition();
            return ContainerTypes.Contains(genericType);
        }

        return false;
    }

    protected virtual Type GetKeyType(Type type)
    {
        if (IsContainerType(type))
        {
            Type typedef = type.GetGenericTypeDefinition();

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

        foreach (KeyValuePair<string, Type> entry in IdClassMapping)
        {
            string id = entry.Key;
            Type type = entry.Value;
            _classIdMapping[type] = id;
        }
    }
}
