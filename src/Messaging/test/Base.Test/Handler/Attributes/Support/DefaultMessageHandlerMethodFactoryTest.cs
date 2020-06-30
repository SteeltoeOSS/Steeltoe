// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test
{
    public class DefaultMessageHandlerMethodFactoryTest
    {
        private readonly SampleBean sample = new SampleBean();

        [Fact]
        public void CustomConversion()
        {
            var conversionService = new GenericConversionService();
            conversionService.AddConverter(new SampleBeanConverter());
            var instance = CreateInstance(conversionService);

            var invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));

            invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(sample).Build());
            AssertMethodInvocation(sample, "SimpleString");
        }

        [Fact]
        public void CustomConversionServiceFailure()
        {
            var conversionService = new GenericConversionService();
            var instance = CreateInstance(conversionService);
            Assert.False(conversionService.CanConvert(typeof(int), typeof(string)));
            var invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));
            Assert.Throws<MessageConversionException>(() => invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(123).Build()));
        }

        [Fact]
        public void CustomMessageConverterFailure()
        {
            IMessageConverter messageConverter = new ByteArrayMessageConverter();
            var instance = CreateInstance(messageConverter);

            var invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));
            Assert.Throws<MessageConversionException>(() => invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(123).Build()));
        }

        [Fact]
        public void CustomArgumentResolver()
        {
            var customResolvers = new List<IHandlerMethodArgumentResolver>();
            customResolvers.Add(new CustomHandlerMethodArgumentResolver());
            var instance = CreateInstance(customResolvers);

            var invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "CustomArgumentResolver", typeof(CultureInfo));

            invocableHandlerMethod.Invoke(MessageBuilder.WithPayload(123).Build());
            AssertMethodInvocation(sample, "CustomArgumentResolver");
        }

        [Fact]
        public void OverrideArgumentResolvers()
        {
            var instance = new DefaultMessageHandlerMethodFactory();
            var customResolvers = new List<IHandlerMethodArgumentResolver>();
            customResolvers.Add(new CustomHandlerMethodArgumentResolver());
            instance.SetArgumentResolvers(customResolvers); // Override defaults

            var message = MessageBuilder.WithPayload("sample").Build();

            // This will work as the local resolver is set
            var invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "CustomArgumentResolver", typeof(CultureInfo));
            invocableHandlerMethod.Invoke(message);
            AssertMethodInvocation(sample, "CustomArgumentResolver");

            // This won't work as no resolver is known for the payload
            var invocableHandlerMethod2 = CreateInvocableHandlerMethod(instance, "SimpleString", typeof(string));
            Assert.Throws<MethodArgumentResolutionException>(() => invocableHandlerMethod2.Invoke(message));
        }

        [Fact]
        public void NoValidationByDefault()
        {
            var instance = new DefaultMessageHandlerMethodFactory();
            var invocableHandlerMethod = CreateInvocableHandlerMethod(instance, "PayloadValidation", typeof(string));
            invocableHandlerMethod.Invoke(MessageBuilder.WithPayload("failure").Build());
            AssertMethodInvocation(sample, "PayloadValidation");
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
            return factory.CreateInvocableHandlerMethod(sample, GetListenerMethod(methodName, parameterTypes));
        }

        private MethodInfo GetListenerMethod(string methodName, params Type[] parameterTypes)
        {
            var method = typeof(SampleBean).GetMethod(methodName, parameterTypes);
            Assert.NotNull(method);
            return method;
        }

        private void AssertMethodInvocation(SampleBean bean, string methodName)
        {
            Assert.True(bean.Invocations[methodName]);
        }

        internal class SampleBeanConverter : AbstractConverter<SampleBean, string>
        {
            public override string Convert(SampleBean soruce)
            {
                return "foo bar";
            }
        }

        internal class SampleBean
        {
            public readonly Dictionary<string, bool> Invocations = new Dictionary<string, bool>();

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

        internal class CustomHandlerMethodArgumentResolver : IHandlerMethodArgumentResolver
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
}
