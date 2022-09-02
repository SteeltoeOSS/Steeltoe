// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Integration.Handler;

public class MethodInvokingMessageProcessorAnnotationTest
{
    private readonly TestService _testService = new();
    private readonly Employee _employee = new("oleg", "zhurakousky");

    [Fact]
    public void OptionalHeader()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(OptionalHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<int?>(context, _testService, method);
        int? result = processor.ProcessMessage(Message.Create("foo"));
        Assert.Null(result);
    }

    [Fact]
    public void RequiredHeaderNotProvided()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.RequiredHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<int>(context, _testService, method);
        Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(Message.Create("foo")));
    }

    [Fact]
    public void RequiredHeaderNotProvidedOnSecondMessage()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.RequiredHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<int>(context, _testService, method);
        IMessage messageWithHeader = MessageBuilder.WithPayload("foo").SetHeader("num", 123).Build();
        IMessage<string> messageWithoutHeader = Message.Create("foo");

        processor.ProcessMessage(messageWithHeader);
        Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(messageWithoutHeader));
    }

    [Fact]
    public void FromMessageWithRequiredHeaderProvided()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.RequiredHeader));
        IApplicationContext context = GetDefaultContext();
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader("num", 123).Build();
        var processor = new MethodInvokingMessageProcessor<int>(context, _testService, method);
        int result = processor.ProcessMessage(message);
        Assert.Equal(123, result);
    }

    [Fact]
    public void FromMessageWithOptionalAndRequiredHeaderAndOnlyOptionalHeaderProvided()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.OptionalAndRequiredHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<string>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader("prop", "bar").Build();
        Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(message));
    }

    [Fact]
    public void FromMessageWithOptionalAndRequiredHeaderAndOnlyRequiredHeaderProvided()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.OptionalAndRequiredHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<string>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader("num", 123).Build();
        string result = processor.ProcessMessage(message);
        Assert.Equal("null123", result);
    }

    [Fact]
    public void FromMessageWithOptionalAndRequiredHeaderAndBothHeadersProvided()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.OptionalAndRequiredHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<string>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader("num", 123).SetHeader("prop", "bar").Build();
        string result = processor.ProcessMessage(message);
        Assert.Equal("bar123", result);
    }

    [Fact]
    public void FromMessageWithMapAndObjectMethod()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.MapHeadersAndPayload));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload("test").SetHeader("prop1", "foo").SetHeader("prop2", "bar").Build();
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
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.MapHeaders));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload("test").SetHeader("attrib1", 123).SetHeader("attrib2", 456).Build();
        var result = (IDictionary<string, object>)processor.ProcessMessage(message);
        Assert.Equal(123, result["attrib1"]);
        Assert.Equal(456, result["attrib2"]);
    }

    [Fact]
    public void FromMessageWithMapMethodAndMapPayload()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.MapPayload));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        var payload = new Dictionary<string, int>
        {
            { "attrib1", 88 },
            { "attrib2", 99 }
        };

        IMessage message = MessageBuilder.WithPayload(payload).SetHeader("attrib1", 123).SetHeader("attrib2", 456).Build();
        var result = (IDictionary<string, int>)processor.ProcessMessage(message);
        Assert.Equal(2, result.Count);
        Assert.Equal(88, result["attrib1"]);
        Assert.Equal(99, result["attrib2"]);
    }

    [Fact]
    public void HeaderAnnotationWithExpression()
    {
        IMessage message = GetMessage();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.HeaderAnnotationWithExpression));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        object result = processor.ProcessMessage(message);
        Assert.Equal("monday", result);
    }

    [Fact]
    public void IrrelevantAnnotation()
    {
        IMessage message = MessageBuilder.WithPayload("foo").Build();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.IrrelevantAnnotation));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        object result = processor.ProcessMessage(message);
        Assert.Equal("foo", result);
    }

    [Fact]
    public void MultipleAnnotatedArgs()
    {
        IMessage message = GetMessage();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.MultipleAnnotatedArguments));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        object[] parameters = (object[])processor.ProcessMessage(message);
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
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.MapOnly));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        IMessage message = MessageBuilder.WithPayload(_employee).SetHeader("number", "jkl").Build();
        var result = processor.ProcessMessage(message) as IDictionary<string, object>;
        Assert.NotNull(result);
        Assert.Equal("jkl", result["number"]);
    }

    [Fact]
    public void FromMessageToPayloadArg()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.PayloadAnnotationFirstName));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        IMessage message = MessageBuilder.WithPayload(_employee).SetHeader("number", "jkl").Build();
        string result = processor.ProcessMessage(message) as string;
        Assert.NotNull(result);
        Assert.Equal("oleg", result);
    }

    [Fact]
    public void FromMessageToPayloadArgs()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.PayloadAnnotationFullName));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);

        IMessage message = MessageBuilder.WithPayload(_employee).SetHeader("number", "jkl").Build();
        object result = processor.ProcessMessage(message);
        Assert.Equal("oleg zhurakousky", result);
    }

    [Fact]
    public void FromMessageToPayloadArgsHeaderArgs()
    {
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.PayloadArgAndHeaderArg));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload(_employee).SetHeader("day", "monday").Build();
        object result = processor.ProcessMessage(message);
        Assert.Equal("olegmonday", result);
    }

    [Fact]
    public void FromMessageInvalidMethodWithMultipleMappingAnnotations()
    {
        MethodInfo method = typeof(MultipleMappingAnnotationTestBean).GetMethod(nameof(MultipleMappingAnnotationTestBean.Test));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload("payload").SetHeader("foo", "bar").Build();
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
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.HeaderNameWithHyphen));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, _testService, method);
        IMessage message = MessageBuilder.WithPayload("payload").SetHeader("foo-bar", "abc").Build();
        object result = processor.ProcessMessage(message);
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
        AbstractMessageBuilder builder = MessageBuilder.WithPayload(_employee);
        builder.SetHeader("day", "monday");
        builder.SetHeader("month", "September");
        return builder.Build();
    }

    public class MultipleMappingAnnotationTestBean
    {
        public void Test([Payload("payload")] [Header("foo")] string s)
        {
        }
    }

    public class TestService
    {
        public ISet<string> Ids { get; } = new HashSet<string>();

        public IDictionary MapOnly(IDictionary map)
        {
            return map;
        }

        public string PayloadAnnotationFirstName([Payload("FirstName")] string name)
        {
            return name;
        }

        public string PayloadAnnotationFullName([Payload("FirstName")] string first, [Payload("LastName")] string last)
        {
            return $"{first} {last}";
        }

        public string PayloadArgAndHeaderArg([Payload("FirstName")] string name, [Header] string day)
        {
            return name + day;
        }

        public int? OptionalHeader([Header(Required = false)] int? num)
        {
            return num;
        }

        public int RequiredHeader([Header(Name = "num", Required = true)] int num)
        {
            return num;
        }

        public string HeadersWithExpressions([Header("emp.FirstName")] string firstName, [Header("emp.LastName.ToUpper()")] string lastName)
        {
            return $"{lastName}, {firstName}";
        }

        public string OptionalAndRequiredHeader([Header(Required = false)] string prop, [Header(Name = "num", Required = true)] int num)
        {
            return (prop ?? "null") + num;
        }

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
        public IDictionary MapPayload(IDictionary map)
        {
            return map;
        }

        public IDictionary<string, object> MapHeaders([Headers] IDictionary<string, object> map)
        {
            return map;
        }

        public object MapHeadersAndPayload(IDictionary headers, object payload)
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

        public int IntegerMethod(int i)
        {
            return i;
        }

        public string HeaderAnnotationWithExpression([Header("day")] string value)
        {
            return value;
        }

        public object[] MultipleAnnotatedArguments([Header("day")] string argA, [Header("month")] string argB, [Payload] Employee payloadArg,
            [Payload("FirstName")] string value, [Headers] IDictionary<string, object> headers)
        {
            return new object[]
            {
                argA,
                argB,
                payloadArg,
                value,
                headers
            };
        }

        public string IrrelevantAnnotation([Bogus] string value)
        {
            return value;
        }

        public string HeaderNameWithHyphen([Header("foo-bar")] string foobar)
        {
            return foobar.ToUpper();
        }

        public string HeaderId(string payload, [Header("id")] string id)
        {
            Ids.Add(id);
            return "foo";
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class BogusAttribute : Attribute
    {
    }

    public class Employee
    {
        public string FirstName { get; }

        public string LastName { get; }

        public Employee(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
    }
}
