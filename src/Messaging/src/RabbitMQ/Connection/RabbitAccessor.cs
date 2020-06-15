// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
