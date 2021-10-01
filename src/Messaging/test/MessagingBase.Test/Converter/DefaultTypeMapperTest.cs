// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Converter
{
    public class DefaultTypeMapperTest
    {
        private readonly DefaultTypeMapper typeMapper = new ();
        private readonly MessageHeaders headers = new ();

        [Fact]
        public void GetAnObjectWhenClassIdNotPresent()
        {
            var type = typeMapper.ToType(headers);
            Assert.Equal(typeof(object), type);
        }

        [Fact]
        public void ShouldLookInTheClassIdFieldNameToFindTheClassName()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader("type", "System.String");
            typeMapper.ClassIdFieldName = "type";

            var type = typeMapper.ToType(accessor.MessageHeaders);
            Assert.Equal(typeof(string), type);
        }

        [Fact]
        public void ShouldUseTheClassProvidedByTheLookupMapIfPresent()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader("__TypeId__", "trade");
            typeMapper.SetIdClassMapping(new Dictionary<string, Type>() { { "trade", typeof(SimpleTrade) } });

            var type = typeMapper.ToType(accessor.MessageHeaders);
            Assert.Equal(typeof(SimpleTrade), type);
        }

        [Fact]
        public void FromTypeShouldPopulateWithTypeNameByDefault()
        {
            typeMapper.FromType(typeof(SimpleTrade), headers);
            var className = headers.Get<string>(typeMapper.ClassIdFieldName);
            Assert.Equal(typeof(SimpleTrade).ToString(), className);
        }

        [Fact]
        public void ShouldUseSpecialNameForClassIfPresent()
        {
            typeMapper.SetIdClassMapping(new Dictionary<string, Type>() { { "daytrade", typeof(SimpleTrade) } });
            typeMapper.FromType(typeof(SimpleTrade), headers);
            var className = headers.Get<string>(typeMapper.ClassIdFieldName);
            Assert.Equal("daytrade", className);
        }

        [Fact]
        public void ShouldThrowAnExceptionWhenContentClassIdIsNotPresentWhenClassIdIsContainerType()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader(typeMapper.ClassIdFieldName, typeof(List<>).FullName);
            var excep = Assert.Throws<MessageConversionException>(() => typeMapper.ToType(accessor.MessageHeaders));
            Assert.Contains("Could not resolve ", excep.Message);
        }

        [Fact]
        public void ShouldLookInTheContentClassIdFieldNameToFindTheContainerClassIDWhenClassIdIsContainerType()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader("contentType", typeof(string).ToString());
            accessor.SetHeader(typeMapper.ClassIdFieldName, typeof(List<>).FullName);
            typeMapper.ContentClassIdFieldName = "contentType";
            var type = typeMapper.ToType(accessor.MessageHeaders);
            Assert.Equal(typeof(List<string>), type);
        }

        [Fact]
        public void ShouldUseTheContentClassProvidedByTheLookupMapIfPresent()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader(typeMapper.ClassIdFieldName, typeof(List<>).FullName);
            accessor.SetHeader("__ContentTypeId__", "trade");
            var mapping = new Dictionary<string, Type>() { { "trade", typeof(SimpleTrade) } };
            mapping.Add(typeMapper.ClassIdFieldName, typeof(List<>));
            typeMapper.SetIdClassMapping(mapping);

            var type = typeMapper.ToType(accessor.MessageHeaders);
            Assert.Equal(typeof(List<SimpleTrade>), type);
        }

        [Fact]
        public void FromTypeShouldPopulateWithContentTypeTypeNameByDefault()
        {
            typeMapper.FromType(typeof(List<SimpleTrade>), headers);

            var className = headers.Get<string>(typeMapper.ClassIdFieldName);
            var contentClassName = headers.Get<string>(typeMapper.ContentClassIdFieldName);
            Assert.Equal(typeof(List<>).FullName, className);
            Assert.Equal(typeof(SimpleTrade).ToString(), contentClassName);
        }

        [Fact]
        public void ShouldThrowAnExceptionWhenKeyClassIdIsNotPresentWhenClassIdIsAMap()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader(typeMapper.ClassIdFieldName, typeof(Dictionary<,>).FullName);
            accessor.SetHeader(typeMapper.KeyClassIdFieldName, typeof(string).ToString());

            var excep = Assert.Throws<MessageConversionException>(() => typeMapper.ToType(accessor.MessageHeaders));
            Assert.Contains("Could not resolve ", excep.Message);
        }

        [Fact]
        public void ShouldLookInTheValueClassIdFieldNameToFindTheValueClassIDWhenClassIdIsAMap()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader("keyType", typeof(int).ToString());
            accessor.SetHeader(typeMapper.ContentClassIdFieldName, typeof(string).ToString());
            accessor.SetHeader(typeMapper.ClassIdFieldName, typeof(Dictionary<,>).FullName);
            typeMapper.KeyClassIdFieldName = "keyType";

            var type = typeMapper.ToType(accessor.MessageHeaders);
            Assert.Equal(typeof(Dictionary<int, string>), type);
        }

        [Fact]
        public void ShouldUseTheKeyClassProvidedByTheLookupMapIfPresent()
        {
            var accessor = MessageHeaderAccessor.GetMutableAccessor(headers);
            accessor.SetHeader("__KeyTypeId__", "trade");
            accessor.SetHeader(typeMapper.ContentClassIdFieldName, typeof(string).ToString());
            accessor.SetHeader(typeMapper.ClassIdFieldName, typeof(Dictionary<,>).FullName);

            var mapping = new Dictionary<string, Type>() { { "trade", typeof(SimpleTrade) } };
            mapping.Add(typeMapper.ClassIdFieldName, typeof(Dictionary<,>));
            mapping.Add(typeMapper.ContentClassIdFieldName, typeof(string));
            typeMapper.SetIdClassMapping(mapping);

            var type = typeMapper.ToType(accessor.MessageHeaders);
            Assert.Equal(typeof(Dictionary<SimpleTrade, string>), type);
        }

        [Fact]
        public void FromTypeShouldPopulateWithKeyTypeAndContentTypeNameByDefault()
        {
            typeMapper.FromType(typeof(Dictionary<SimpleTrade, string>), headers);

            var className = headers.Get<string>(typeMapper.ClassIdFieldName);
            var contentClassName = headers.Get<string>(typeMapper.ContentClassIdFieldName);
            var keyClassName = headers.Get<string>(typeMapper.KeyClassIdFieldName);
            Assert.Equal(typeof(Dictionary<,>).FullName, className);
            Assert.Equal(typeof(SimpleTrade).ToString(), keyClassName);
            Assert.Equal(typeof(string).ToString(), contentClassName);
        }

        [Fact]
        public void RoundTrip()
        {
            typeMapper.FromType(typeof(Dictionary<SimpleTrade, string>), headers);
            var type = typeMapper.ToType(headers);
            Assert.Equal(typeof(Dictionary<SimpleTrade, string>), type);
        }
    }
}
