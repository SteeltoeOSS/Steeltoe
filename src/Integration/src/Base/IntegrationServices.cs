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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;

namespace Steeltoe.Integration
{
    public class IntegrationServices : IIntegrationServices
    {
        protected IMessageBuilderFactory _messageBuilderFactory;
        protected IConversionService _conversionService;
        protected IIDGenerator _idGenerator;
        protected IDestinationResolver<IMessageChannel> _channelResolver;
        protected IApplicationContext _context;

        public IntegrationServices(IApplicationContext context)
        {
            _context = context;
        }

        public virtual IMessageBuilderFactory MessageBuilderFactory
        {
            get
            {
                if (_messageBuilderFactory == null)
                {
                    _messageBuilderFactory = _context.GetService<IMessageBuilderFactory>();
                }

                return _messageBuilderFactory;
            }

            set
            {
                _messageBuilderFactory = value;
            }
        }

        public virtual IDestinationResolver<IMessageChannel> ChannelResolver
        {
            get
            {
                if (_channelResolver == null)
                {
                    _channelResolver = _context.GetService<IDestinationResolver<IMessageChannel>>();
                }

                return _channelResolver;
            }

            set
            {
                _channelResolver = value;
            }
        }

        public virtual IConversionService ConversionService
        {
            get
            {
                if (_conversionService == null)
                {
                    _conversionService = _context.GetService<IConversionService>();
                }

                return _conversionService;
            }

            set
            {
                _conversionService = value;
            }
        }

        public virtual IIDGenerator IdGenerator
        {
            get
            {
                if (_idGenerator == null)
                {
                    _idGenerator = _context.GetService<IIDGenerator>();
                }

                return _idGenerator;
            }

            set
            {
                _idGenerator = value;
            }
        }
    }
}
