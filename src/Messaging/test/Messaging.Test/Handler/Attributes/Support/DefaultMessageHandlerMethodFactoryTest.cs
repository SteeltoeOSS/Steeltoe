// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test;

public class DefaultMessageHandlerMethodFactoryTest
{
    private readonly SampleBean _sample = new();

    [Fact]
    public void CustomConversion()
    {
        var conversionService = new GenericConversionService();
        conversionService.AddConverter(new SampleBeanConverter());
        DefaultMessageHandlerMethodFactory instance = CreateInstance(conversionService);

        IInvocableHandlerMethod invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));

        invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(_sample).Build());
        AssertMethodInvocation(_sample, "SimpleString");
    }

    [Fact]
    public void CustomConversionServiceFailure()
    {
        var conversionService = new GenericConversionService();
        DefaultMessageHandlerMethodFactory instance = CreateInstance(conversionService);
        Assert.False(conversionService.CanConvert(typeof(int), typeof(string)));
        IInvocableHandlerMethod invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));
        Assert.Throws<MessageConversionException>(() => invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(123).Build()));
    }

    [Fact]
    public void CustomMessageConverterFailure()
    {
        IMessageConverter messageConverter = new ByteArrayMessageConverter();
        DefaultMessageHandlerMethodFactory instance = CreateInstance(messageConverter);

        IInvocableHandlerMethod invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));
        Assert.Throws<MessageConversionException>(() => invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(123).Build()));
    }

    [Fact]
    public void CustomArgumentResolver()
    {
        var customResolvers = new List<IHandlerMethodArgumentResolver>
        {
            new CustomHandlerMethodArgumentResolver()
        };

        DefaultMessageHandlerMethodFactory instance = CreateInstance(customResolvers);

        IInvocableHandlerMethod invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "CustomArgumentResolver", typeof(CultureInfo));

        invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(123).Build());
        AssertMethodInvocation(_sample, "CustomArgumentResolver");
    }

    [Fact]
    public void OverrideArgumentResolvers()
    {
        var instance = new DefaultMessageHandlerMethodFactory();

        var customResolvers = new List<IHandlerMethodArgumentResolver>
        {
            new CustomHandlerMethodArgumentResolver()
        };

        instance.SetArgumentResolvers(customResolvers); // Override defaults

        IMessage message = MessageBuilder.WithPayload("sample").Build();

        // This will work as the local resolver is set
        IInvocableHandlerMethod invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "CustomArgumentResolver", typeof(CultureInfo));
        invocableHandlerMethod.Invoke(message);
        AssertMethodInvocation(_sample, "CustomArgumentResolver");

        // This won't work as no resolver is known for the payload
        IInvocableHandlerMethod invocableHandlerMethod2 = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));
        Assert.Throws<MethodArgumentResolutionException>(() => invocableHandlerMethod2.Invoke(message));
    }

    [Fact]
    public void NoValidationByDefault()
    {
        var instance = new DefaultMessageHandlerMethodFactory();
        IInvocableHandlerMethod invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "PayloadValidation", typeof(string));
        invocableHandlerMethod.Invoke(MessageBuilder.WithPayload("failure").Build());
        AssertMethodInvocation(_sample, "PayloadValidation");
    }

    private DefaultMessageHandlerMethodFactory CreateInstance(List<IHandlerMethodArgumentResolver> customResolvers)
    {
        var factory = new DefaultMessageHandlerMethodFactory(null, null, customResolvers);
        return factory;
    }

    private DefaultMessageHandlerMethodFactory CreateInstance(IMessageConverter converter)
    {
        var factory = new DefaultMessageHandlerMethodFactory(null, converter);
        return factory;
    }

    private DefaultMessageHandlerMethodFactory CreateInstance(GenericConversionService conversionService)
    {
        var factory = new DefaultMessageHandlerMethodFactory(conversionService);
        return factory;
    }

    private IInvocableHandlerMethod CreateInvocableHandlerMethod(DefaultMessageHandlerMethodFactory factory, string methodName, params Type[] parameterTypes)
    {
        return factory.CreateInvocableHandlerMethod(_sample, GetListenerMethod(methodName, parameterTypes));
    }

    private MethodInfo GetListenerMethod(string methodName, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(SampleBean).GetMethod(methodName, parameterTypes);
        Assert.NotNull(method);
        return method;
    }

    private void AssertMethodInvocation(SampleBean bean, string methodName)
    {
        Assert.True(bean.Invocations[methodName]);
    }

    internal sealed class SampleBeanConverter : AbstractConverter<SampleBean, string>
    {
        public override string Convert(SampleBean source)
        {
            return "foo bar";
        }
    }

    internal sealed class SampleBean
    {
        public Dictionary<string, bool> Invocations { get; } = new();

        public void SimpleString(string value)
        {
            Invocations.Add("SimpleString", true);
        }

        public void PayloadValidation([Payload] string value)
        {
            Invocations.Add("PayloadValidation", true);
        }

        public void CustomArgumentResolver(CultureInfo locale)
        {
            Invocations.Add("CustomArgumentResolver", true);
            Assert.Equal(CultureInfo.CurrentCulture, locale);
        }
    }

    internal sealed class CustomHandlerMethodArgumentResolver : IHandlerMethodArgumentResolver
    {
        public bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.ParameterType.IsAssignableFrom(typeof(CultureInfo));
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            return CultureInfo.CurrentCulture;
        }
    }
}
