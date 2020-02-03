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

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class DefaultMessageHandlerMethodFactory : IMessageHandlerMethodFactory
    {
        protected readonly HandlerMethodArgumentResolverComposite _argumentResolvers = new HandlerMethodArgumentResolverComposite();

        public IConversionService ConversionService { get; set; } = new GenericConversionService();

        public IMessageConverter MessageConverter { get; set; }

        public List<IHandlerMethodArgumentResolver> CustomArgumentResolvers { get; set; }

        public DefaultMessageHandlerMethodFactory()
        {
            AfterPropertiesSet();
        }

        internal DefaultMessageHandlerMethodFactory(IConversionService conversionService)
        {
            ConversionService = conversionService;
            AfterPropertiesSet();
        }

        internal DefaultMessageHandlerMethodFactory(IConversionService conversionService, IMessageConverter converter)
        {
            ConversionService = conversionService;
            MessageConverter = converter;
            AfterPropertiesSet();
        }

        internal DefaultMessageHandlerMethodFactory(IConversionService conversionService, IMessageConverter converter, List<IHandlerMethodArgumentResolver> resolvers)
        {
            ConversionService = conversionService;
            MessageConverter = converter;
            CustomArgumentResolvers = resolvers;
            AfterPropertiesSet();
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

        protected void AfterPropertiesSet()
        {
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

        protected virtual List<IHandlerMethodArgumentResolver> InitArgumentResolvers()
        {
            var resolvers = new List<IHandlerMethodArgumentResolver>();

            // ConfigurableBeanFactory beanFactory = (this.beanFactory instanceof ConfigurableBeanFactory ?

            // (ConfigurableBeanFactory)this.beanFactory : null);

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
