// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Integration.Handler;

public class MethodInvokingMessageProcessorAnnotationTest
{
    private static volatile int _concurrencyFailures;
    private readonly TestService _testService = new ();
    private readonly Employee _employee = new ("oleg", "zhurakousky");

    [Fact]
    public void OptionalHeader()
    {
        var method = typeof(TestService).GetMethod(nameof(OptionalHeader));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<int?>(context, _testService, method);
        var result = processor.ProcessMessage(Message.Create("foo"));
        Assert.Null(result);
    }

    [Fact]
    public void RequiredHeaderNotProvided()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.RequiredHeader));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<int>(context, _testService, method);
        Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(Message.Create("foo")));
    }

    [Fact]
    public void RequiredHeaderNotProvidedOnSecondMessage()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.RequiredHeader));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<int>(context, _testService, method);
        var messageWithHeader = MessageBuilder.WithPayload("foo").SetHeader("num", 123).Build();
        var messageWithoutHeader = Message.Create("foo");

        processor.ProcessMessage(messageWithHeader);
        Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(messageWithoutHeader));
    }

    [Fact]
    public void FromMessageWithRequiredHeaderProvided()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.RequiredHeader));
        var context = GetDefaultContext();
        var message = MessageBuilder.WithPayload("foo").SetHeader("num", 123).Build();
        var processor = new MethodInvokingMessageProcessor<int>(context, _testService, method);
        var result = processor.ProcessMessage(message);
        Assert.Equal(123, result);
    }

    [Fact]
    public void FromMessageWithOptionalAndRequiredHeaderAndOnlyOptionalHeaderProvided()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.OptionalAndRequiredHeader));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<string>(context, _testService, method);
        var message = MessageBuilder.WithPayload("foo").SetHeader("prop", "bar").Build();
        Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(message));
    }

    [Fact]
    public void FromMessageWithOptionalAndRequiredHeaderAndOnlyRequiredHeaderProvided()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.OptionalAndRequiredHeader));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<string>(context, _testService, method);
        var message = MessageBuilder.WithPayload("foo").SetHeader("num", 123).Build();
        var result = processor.ProcessMessage(message);
        Assert.Equal("null123", result);
    }

    [Fact]
    public void FromMessageWithOptionalAndRequiredHeaderAndBothHeadersProvided()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.OptionalAndRequiredHeader));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<string>(context, _testService, method);
        var message = MessageBuilder.WithPayload("foo")
            .SetHeader("num", 123)
            .SetHeader("prop", "bar")
            .Build();
        var result = processor.ProcessMessage(message);
        Assert.Equal("bar123", result);
    }

    [Fact]
    public void FromMessageWithMapAndObjectMethod()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.MapHeadersAndPayload));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        var message = MessageBuilder.WithPayload("test")
            .SetHeader("prop1", "foo")
            .SetHeader("prop2", "bar")
            .Build();
        var result = (IDictionary<object, object>)processor.ProcessMessage(message);
        Assert.Equal(5, result.Count);
        Assert.True(result.ContainsKey(MessageHeaders.IdName));
        Assert.True(result.ContainsKey(MessageHeaders.TimestampName));
        Assert.Equal("foo", result["prop1"]);
        Assert.Equal("bar", result["prop2"]);
        Assert.Equal("test", result["payload"]);
    }

    [Fact]
    public void FromMessageWithMapMethodAndHeadersAnnotation()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.MapHeaders));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        var message = MessageBuilder.WithPayload("test")
            .SetHeader("attrib1", 123)
            .SetHeader("attrib2", 456)
            .Build();
        var result = (IDictionary<string, object>)processor.ProcessMessage(message);
        Assert.Equal(123, result["attrib1"]);
        Assert.Equal(456, result["attrib2"]);
    }

    [Fact]
    public void FromMessageWithMapMethodAndMapPayload()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.MapPayload));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        var payload = new Dictionary<string, int>
        {
            { "attrib1", 88 },
            { "attrib2", 99 }
        };
        var message = MessageBuilder.WithPayload(payload)
            .SetHeader("attrib1", 123)
            .SetHeader("attrib2", 456)
            .Build();
        var result = (IDictionary<string, int>)processor.ProcessMessage(message);
        Assert.Equal(2, result.Count);
        Assert.Equal(88, result["attrib1"]);
        Assert.Equal(99, result["attrib2"]);
    }

    [Fact]
    public void HeaderAnnotationWithExpression()
    {
        var message = GetMessage();
        var method = typeof(TestService).GetMethod(nameof(TestService.HeaderAnnotationWithExpression));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        var result = processor.ProcessMessage(message);
        Assert.Equal("monday", result);
    }

    [Fact]
    public void IrrelevantAnnotation()
    {
        var message = MessageBuilder.WithPayload("foo").Build();
        var method = typeof(TestService).GetMethod(nameof(TestService.IrrelevantAnnotation));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        var result = processor.ProcessMessage(message);
        Assert.Equal("foo", result);
    }

    [Fact]
    public void MultipleAnnotatedArgs()
    {
        var message = GetMessage();
        var method = typeof(TestService).GetMethod(nameof(TestService.MultipleAnnotatedArguments));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        var parameters = (object[])processor.ProcessMessage(message);
        Assert.NotNull(parameters);
        Assert.Equal(5, parameters.Length);
        Assert.Equal("monday", parameters[0]);
        Assert.Equal("September", parameters[1]);
        Assert.Equal(parameters[2], _employee);
        Assert.Equal("oleg", parameters[3]);
        Assert.True(parameters[4] is IDictionary<string, object>);
    }

    [Fact]
    public void FromMessageToPayload()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.MapOnly));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        var message = MessageBuilder.WithPayload(_employee).SetHeader("number", "jkl").Build();
        var result = processor.ProcessMessage(message) as IDictionary<string, object>;
        Assert.NotNull(result);
        Assert.Equal("jkl", result["number"]);
    }

    [Fact]
    public void FromMessageToPayloadArg()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.PayloadAnnotationFirstName));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        var message = MessageBuilder.WithPayload(_employee).SetHeader("number", "jkl").Build();
        var result = processor.ProcessMessage(message) as string;
        Assert.NotNull(result);
        Assert.Equal("oleg", result);
    }

    [Fact]
    public void FromMessageToPayloadArgs()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.PayloadAnnotationFullName));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        var message = MessageBuilder.WithPayload(_employee).SetHeader("number", "jkl").Build();
        var result = processor.ProcessMessage(message);
        Assert.Equal("oleg zhurakousky", result);
    }

    [Fact]
    public void FromMessageToPayloadArgsHeaderArgs()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.PayloadArgAndHeaderArg));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        var message = MessageBuilder.WithPayload(_employee).SetHeader("day", "monday").Build();
        var result = processor.ProcessMessage(message);
        Assert.Equal("olegmonday", result);
    }

    [Fact]
    public void FromMessageInvalidMethodWithMultipleMappingAnnotations()
    {
        var method = typeof(MultipleMappingAnnotationTestBean).GetMethod(nameof(MultipleMappingAnnotationTestBean.Test));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        var message = MessageBuilder.WithPayload("payload").SetHeader("foo", "bar").Build();
        Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(message));
    }

    // [Fact]
    // public void FromMessageToHeadersWithExpressions()
    // {
    //    var method = typeof(TestService).GetMethod(nameof(TestService.HeadersWithExpressions));
    //    var context = GetDefaultContext();
    //    var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
    //    var employee = new Employee("John", "Doe");
    //    var message = MessageBuilder.WithPayload("payload").SetHeader("emp", employee).Build();
    //    var result = processor.ProcessMessage(message);
    //    Assert.Equal("DOE, John", result);
    // }
    [Fact]
    public void FromMessageToHyphenatedHeaderName()
    {
        var method = typeof(TestService).GetMethod(nameof(TestService.HeaderNameWithHyphen));
        var context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        var message = MessageBuilder.WithPayload("payload").SetHeader("foo-bar", "abc").Build();
        var result = processor.ProcessMessage(message);
        Assert.Equal("ABC", result);
    }

    private IApplicationContext GetDefaultContext()
    {
        var serviceCollection = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        var context = new GenericApplicationContext(serviceCollection.BuildServiceProvider(), configBuilder.Build());
        context.ServiceExpressionResolver = new StandardServiceExpressionResolver();
        return context;
    }

    private IMessage GetMessage()
    {
        var builder = MessageBuilder.WithPayload(_employee);
        builder.SetHeader("day", "monday");
        builder.SetHeader("month", "September");
        return builder.Build();
    }

    public class MultipleMappingAnnotationTestBean
    {
        public void Test([Payload("payload")][Header("foo")] string s)
        {
        }
    }

    public class TestService
    {
        public ISet<string> Ids = new HashSet<string>();

        public System.Collections.IDictionary MapOnly(System.Collections.IDictionary map) => map;

        public string PayloadAnnotationFirstName([Payload("FirstName")] string name) => name;

        public string PayloadAnnotationFullName([Payload("FirstName")] string first, [Payload("LastName")] string last) => $"{first} {last}";

        public string PayloadArgAndHeaderArg([Payload("FirstName")] string name, [Header] string day) => name + day;

        public int? OptionalHeader([Header(Required = false)] int? num) => num;

        public int RequiredHeader([Header(Name = "num", Required = true)] int num) => num;

        public string HeadersWithExpressions([Header("emp.FirstName")] string firstName, [Header("emp.LastName.ToUpper()")] string lastName) => $"{lastName}, {firstName}";

        public string OptionalAndRequiredHeader([Header(Required = false)] string prop, [Header(Name = "num", Required = true)] int num) => (prop ?? "null") + num;

        // public Properties propertiesPayload(Properties properties)
        // {
        //    return properties;
        // }

        // public Properties propertiesHeaders(@Headers Properties properties)
        // {
        //    return properties;
        // }

        // public Object propertiesHeadersAndPayload(Properties headers, Object payload)
        // {
        //    headers.put("payload", payload);
        //    return headers;
        // }
        public System.Collections.IDictionary MapPayload(System.Collections.IDictionary map) => map;

        public IDictionary<string, object> MapHeaders([Headers] IDictionary<string, object> map) => map;

        public object MapHeadersAndPayload(System.Collections.IDictionary headers, object payload)
        {
            // var map = new Dictionary<string, object>(headers);
            var map = new Dictionary<object, object>();
            foreach (KeyValuePair<string, object> kvp in headers)
            {
                map.Add(kvp.Key, kvp.Value);
            }

            map.Add("payload", payload);
            return map;
        }

        public int IntegerMethod(int i) => i;

        public string HeaderAnnotationWithExpression([Header("day")] string value) => value;

        public object[] MultipleAnnotatedArguments([Header("day")] string argA, [Header("month")] string argB, [Payload] Employee payloadArg, [Payload("FirstName")] string value, [Headers] IDictionary<string, object> headers)
            => new object[] { argA, argB, payloadArg, value, headers };

        public string IrrelevantAnnotation([Bogus] string value) => value;

        public string HeaderNameWithHyphen([Header("foo-bar")] string foobar) => foobar.ToUpper();

        public string HeaderId(string payload, [Header("id")] string id)
        {
            // logger.debug(id);
            if (Ids.Contains(id))
            {
                _concurrencyFailures++;
            }

            Ids.Add(id);
            return "foo";
        }
    }

    public class BogusAttribute : Attribute
    {
    }

    public class Employee
    {
        public Employee(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string FirstName { get; }

        public string LastName { get; }
    }
}
