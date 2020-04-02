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

using Steeltoe.Messaging.Rabbit.Support;
using System;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class RabbitAccessor
    {
        public RabbitAccessor(IConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            ConnectionFactory = connectionFactory;
        }

        public virtual IConnectionFactory ConnectionFactory { get; set; }

        public virtual bool IsChannelTransacted { get; set; }

        protected virtual IConnection CreateConnection()
        {
            return ConnectionFactory.CreateConnection();
        }

        protected virtual RabbitResourceHolder GetTransactionalResourceHolder()
        {
            return ConnectionFactoryUtils.GetTransactionalResourceHolder(ConnectionFactory, IsChannelTransacted);
        }

        protected virtual Exception ConvertRabbitAccessException(Exception ex)
        {
            return RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
        }
    }
}
