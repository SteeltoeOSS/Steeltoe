// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class DefaultMessageHandlerMethodFactory : IMessageHandlerMethodFactory
    {
        public const string DEFAULT_SERVICE_NAME = nameof(DefaultMessageHandlerMethodFactory);

        protected readonly HandlerMethodArgumentResolverComposite _argumentResolvers = new HandlerMethodArgumentResolverComposite();

        public virtual string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public virtual IConversionService ConversionService { get; set; }

        public virtual IMessageConverter MessageConverter { get; set; }

        public virtual List<IHandlerMethodArgumentResolver> CustomArgumentResolvers { get; set; }

        public DefaultMessageHandlerMethodFactory()
            : this(null, null, null)
        {
        }

        public DefaultMessageHandlerMethodFactory(IConversionService conversionService)
           : this(conversionService, null, null)
        {
            ConversionService = conversionService;
        }

        public DefaultMessageHandlerMethodFactory(IConversionService conversionService, IMessageConverter converter)
            : this(conversionService, converter, null)
        {
            ConversionService = conversionService;
            MessageConverter = converter;
        }

        public DefaultMessageHandlerMethodFactory(IConversionService conversionService, IMessageConverter converter, List<IHandlerMethodArgumentResolver> resolvers)
        {
            ConversionService = conversionService;
            MessageConverter = converter;
            CustomArgumentResolvers = resolvers;

            if (ConversionService == null)
            {
                ConversionService = new GenericConversionService();
            }

            if (MessageConverter == null)
            {
                MessageConverter = new GenericMessageConverter(ConversionService);
            }

            if (_argumentResolvers.Resolvers.Count == 0)
            {
                _argumentResolvers.AddResolvers(InitArgumentResolvers());
            }
        }

        public virtual void SetArgumentResolvers(List<IHandlerMethodArgumentResolver> argumentResolvers)
        {
            if (argumentResolvers == null)
            {
                _argumentResolvers.Clear();
                return;
            }

            if (argumentResolvers.Count > 0)
            {
                _argumentResolvers.Clear();
            }

            _argumentResolvers.AddResolvers(argumentResolvers);
        }

        public virtual IInvocableHandlerMethod CreateInvocableHandlerMethod(object bean, MethodInfo method)
        {
            var handlerMethod = new InvocableHandlerMethod(bean, method);
            handlerMethod.MessageMethodArgumentResolvers = _argumentResolvers;
            return handlerMethod;
        }

        public virtual void Initialize()
        {
            _argumentResolvers.Clear();

            if (ConversionService == null)
            {
                ConversionService = new GenericConversionService();
            }

            if (MessageConverter == null)
            {
                MessageConverter = new GenericMessageConverter(ConversionService);
            }

            if (_argumentResolvers.Resolvers.Count == 0)
            {
                _argumentResolvers.AddResolvers(InitArgumentResolvers());
            }
        }

        protected List<IHandlerMethodArgumentResolver> InitArgumentResolvers()
        {
            var resolvers = new List<IHandlerMethodArgumentResolver>();

            // Annotation-based argument resolution
            resolvers.Add(new HeaderMethodArgumentResolver(ConversionService));
            resolvers.Add(new HeadersMethodArgumentResolver());

            // Type-based argument resolution
            resolvers.Add(new MessageMethodArgumentResolver(MessageConverter));

            if (CustomArgumentResolvers != null)
            {
                resolvers.AddRange(CustomArgumentResolvers);
            }

            resolvers.Add(new PayloadMethodArgumentResolver(MessageConverter));

            return resolvers;
        }
    }
}
