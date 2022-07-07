// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Converter;

public class DefaultTypeMapperTest
{
    private readonly DefaultTypeMapper _typeMapper = new ();
    private readonly MessageHeaders _headers = new ();

    [Fact]
    public void GetAnObjectWhenClassIdNotPresent()
    {
        var type = _typeMapper.ToType(_headers);
        Assert.Equal(typeof(object), type);
    }

    [Fact]
    public void ShouldLookInTheClassIdFieldNameToFindTheClassName()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader("type", "System.String");
        _typeMapper.ClassIdFieldName = "type";

        var type = _typeMapper.ToType(accessor.MessageHeaders);
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void ShouldUseTheClassProvidedByTheLookupMapIfPresent()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader("__TypeId__", "trade");
        _typeMapper.SetIdClassMapping(new Dictionary<string, Type> { { "trade", typeof(SimpleTrade) } });

        var type = _typeMapper.ToType(accessor.MessageHeaders);
        Assert.Equal(typeof(SimpleTrade), type);
    }

    [Fact]
    public void FromTypeShouldPopulateWithTypeNameByDefault()
    {
        _typeMapper.FromType(typeof(SimpleTrade), _headers);
        var className = _headers.Get<string>(_typeMapper.ClassIdFieldName);
        Assert.Equal(typeof(SimpleTrade).ToString(), className);
    }

    [Fact]
    public void ShouldUseSpecialNameForClassIfPresent()
    {
        _typeMapper.SetIdClassMapping(new Dictionary<string, Type> { { "daytrade", typeof(SimpleTrade) } });
        _typeMapper.FromType(typeof(SimpleTrade), _headers);
        var className = _headers.Get<string>(_typeMapper.ClassIdFieldName);
        Assert.Equal("daytrade", className);
    }

    [Fact]
    public void ShouldThrowAnExceptionWhenContentClassIdIsNotPresentWhenClassIdIsContainerType()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader(_typeMapper.ClassIdFieldName, typeof(List<>).FullName);
        var excep = Assert.Throws<MessageConversionException>(() => _typeMapper.ToType(accessor.MessageHeaders));
        Assert.Contains("Could not resolve ", excep.Message);
    }

    [Fact]
    public void ShouldLookInTheContentClassIdFieldNameToFindTheContainerClassIDWhenClassIdIsContainerType()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader("contentType", typeof(string).ToString());
        accessor.SetHeader(_typeMapper.ClassIdFieldName, typeof(List<>).FullName);
        _typeMapper.ContentClassIdFieldName = "contentType";
        var type = _typeMapper.ToType(accessor.MessageHeaders);
        Assert.Equal(typeof(List<string>), type);
    }

    [Fact]
    public void ShouldUseTheContentClassProvidedByTheLookupMapIfPresent()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader(_typeMapper.ClassIdFieldName, typeof(List<>).FullName);
        accessor.SetHeader("__ContentTypeId__", "trade");
        var mapping = new Dictionary<string, Type>
        {
            { "trade", typeof(SimpleTrade) },
            { _typeMapper.ClassIdFieldName, typeof(List<>) }
        };
        _typeMapper.SetIdClassMapping(mapping);

        var type = _typeMapper.ToType(accessor.MessageHeaders);
        Assert.Equal(typeof(List<SimpleTrade>), type);
    }

    [Fact]
    public void FromTypeShouldPopulateWithContentTypeTypeNameByDefault()
    {
        _typeMapper.FromType(typeof(List<SimpleTrade>), _headers);

        var className = _headers.Get<string>(_typeMapper.ClassIdFieldName);
        var contentClassName = _headers.Get<string>(_typeMapper.ContentClassIdFieldName);
        Assert.Equal(typeof(List<>).FullName, className);
        Assert.Equal(typeof(SimpleTrade).ToString(), contentClassName);
    }

    [Fact]
    public void ShouldThrowAnExceptionWhenKeyClassIdIsNotPresentWhenClassIdIsAMap()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader(_typeMapper.ClassIdFieldName, typeof(Dictionary<,>).FullName);
        accessor.SetHeader(_typeMapper.KeyClassIdFieldName, typeof(string).ToString());

        var excep = Assert.Throws<MessageConversionException>(() => _typeMapper.ToType(accessor.MessageHeaders));
        Assert.Contains("Could not resolve ", excep.Message);
    }

    [Fact]
    public void ShouldLookInTheValueClassIdFieldNameToFindTheValueClassIDWhenClassIdIsAMap()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader("keyType", typeof(int).ToString());
        accessor.SetHeader(_typeMapper.ContentClassIdFieldName, typeof(string).ToString());
        accessor.SetHeader(_typeMapper.ClassIdFieldName, typeof(Dictionary<,>).FullName);
        _typeMapper.KeyClassIdFieldName = "keyType";

        var type = _typeMapper.ToType(accessor.MessageHeaders);
        Assert.Equal(typeof(Dictionary<int, string>), type);
    }

    [Fact]
    public void ShouldUseTheKeyClassProvidedByTheLookupMapIfPresent()
    {
        var accessor = MessageHeaderAccessor.GetMutableAccessor(_headers);
        accessor.SetHeader("__KeyTypeId__", "trade");
        accessor.SetHeader(_typeMapper.ContentClassIdFieldName, typeof(string).ToString());
        accessor.SetHeader(_typeMapper.ClassIdFieldName, typeof(Dictionary<,>).FullName);

        var mapping = new Dictionary<string, Type>
        {
            { "trade", typeof(SimpleTrade) },
            { _typeMapper.ClassIdFieldName, typeof(Dictionary<,>) },
            { _typeMapper.ContentClassIdFieldName, typeof(string) }
        };
        _typeMapper.SetIdClassMapping(mapping);

        var type = _typeMapper.ToType(accessor.MessageHeaders);
        Assert.Equal(typeof(Dictionary<SimpleTrade, string>), type);
    }

    [Fact]
    public void FromTypeShouldPopulateWithKeyTypeAndContentTypeNameByDefault()
    {
        _typeMapper.FromType(typeof(Dictionary<SimpleTrade, string>), _headers);

        var className = _headers.Get<string>(_typeMapper.ClassIdFieldName);
        var contentClassName = _headers.Get<string>(_typeMapper.ContentClassIdFieldName);
        var keyClassName = _headers.Get<string>(_typeMapper.KeyClassIdFieldName);
        Assert.Equal(typeof(Dictionary<,>).FullName, className);
        Assert.Equal(typeof(SimpleTrade).ToString(), keyClassName);
        Assert.Equal(typeof(string).ToString(), contentClassName);
    }

    [Fact]
    public void RoundTrip()
    {
        _typeMapper.FromType(typeof(Dictionary<SimpleTrade, string>), _headers);
        var type = _typeMapper.ToType(_headers);
        Assert.Equal(typeof(Dictionary<SimpleTrade, string>), type);
    }
}
