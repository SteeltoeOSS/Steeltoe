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

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public static class RabbitUtils
    {
        public const int ReplySuccess = 200;
        public const int NotFound = 404;
        public const int Precondition_Failed = 406;

        public const ushort Exchange_ClassId = 40;
        public const ushort Queue_ClassId = 50;
        public const ushort Declare_MethodId = 10;
        public const ushort ChannelClose_ClassId = 20;
        public const ushort ChannelClose_MethodId = 40;
        public const ushort ConnectionClose_ClassId = 10;
        public const ushort ConnectionClose_MethodId = 50;

        private static readonly AsyncLocal<bool?> _physicalCloseRequired = new AsyncLocal<bool?>();

        public static void CloseConnection(IConnection connection, ILogger logger = null)
        {
            if (connection != null)
            {
                try
                {
                    connection.Close();
                }
                catch (AlreadyClosedException)
                {
                    // empty
                }
                catch (Exception ex)
                {
                    logger?.LogDebug("Ignoring Connection exception - assuming already closed: " + ex.Message, ex);
                }
            }
        }

        public static void CloseChannel(IModel channel, ILogger logger = null)
        {
            if (channel != null)
            {
                try
                {
                    channel.Close();
                }
                catch (AlreadyClosedException ace)
                {
                    // empty
                }
                catch (IOException ex)
                {
                    logger?.LogDebug("Could not close RabbitMQ Channel", ex);
                }
                catch (ShutdownSignalException sig)
                {
                    if (!IsNormalShutdown(sig))
                    {
                        logger?.LogDebug("Unexpected exception on closing RabbitMQ Channel", sig);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogDebug("Unexpected exception on closing RabbitMQ Channel", ex);
                }
            }
        }

        public static void CommitIfNecessary(IModel channel, ILogger logger = null)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            try
            {
                channel.TxCommit();
            }
            catch (IOException ex)
            {
                logger.LogError("Error during TxCommit", ex);
                throw new AmqpIOException(ex);
            }
        }

        public static void RollbackIfNecessary(IModel channel, ILogger logger = null)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            try
            {
                channel.TxRollback();
            }
            catch (IOException ex)
            {
                logger.LogError("Error during TxCommit", ex);
                throw new AmqpIOException(ex);
            }
        }

        public static void CloseMessageConsumer(IModel channel, List<string> consumerTags, bool transactional, ILogger logger = null)
        {
            if (!channel.IsOpen)
            {
                return;
            }

            try
            {
                foreach (var consumerTag in consumerTags)
                {
                    Cancel(channel, consumerTag);
                }

                if (transactional)
                {
                    // Re-queue in-flight messages if any (after the consumer is cancelled to prevent the broker from simply
                    // sending them back to us). Does not require a tx.commit.
                    channel.BasicRecover(true);
                }

                // If not transactional then we are auto-acking (at least as of 1.0.0.M2) so there is nothing to recover.
                // Messages are going to be lost in general.
            }
            catch (Exception ex)
            {
                logger?.LogError("Exception during CloseMessageConsumer", ex);

                RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
            }
        }

        public static void Cancel(IModel channel, string consumerTag, ILogger logger = null)
        {
            try
            {
                channel.BasicCancel(consumerTag);
            }
            catch (AlreadyClosedException e)
            {
                logger?.LogTrace(channel + " is already closed", e);
            }
            catch (Exception e)
            {
                logger?.LogDebug("Error performing 'basicCancel' on " + channel, e);
            }
        }

        public static void DeclareTransactional(IModel channel, ILogger logger = null)
        {
            try
            {
                channel.TxSelect();
            }
            catch (IOException e)
            {
                logger?.LogDebug("Error performing 'txSelect' on " + channel, e);
                RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        public static void SetPhysicalCloseRequired(IModel channel, bool b)
        {
            if (channel is IChannelProxy asProxy)
            {
                _physicalCloseRequired.Value = true;
            }
        }

        public static bool IsPhysicalCloseRequired()
        {
            var mustClose = _physicalCloseRequired.Value;
            if (!mustClose.HasValue)
            {
                mustClose = false;
            }
            else
            {
                _physicalCloseRequired.Value = null;
            }

            return mustClose.Value;
        }

        public static bool IsNormalChannelClose(ShutdownEventArgs args)
        {
            return IsNormalShutdown(args) ||
                   (args.ClassId == ChannelClose_ClassId
                    && args.MethodId == ChannelClose_MethodId
                    && args.ReplyCode == ReplySuccess
                    && args.ReplyText == "OK");
        }

        public static bool IsNormalShutdown(ShutdownSignalException sig)
        {
            return sig.ClassId == ConnectionClose_ClassId
                    && sig.MethodId == ConnectionClose_MethodId
                    && sig.ReplyCode == ReplySuccess
                    && sig.ReplyText == "OK";
        }

        public static bool IsNormalShutdown(ShutdownEventArgs args)
        {
            return args.ClassId == ConnectionClose_ClassId
                    && args.MethodId == ConnectionClose_MethodId
                    && args.ReplyCode == ReplySuccess
                    && args.ReplyText == "OK";
        }

        public static bool IsPassiveDeclarationChannelClose(ShutdownSignalException cause)
        {
            return (cause.ClassId == Exchange_ClassId || cause.ClassId == Queue_ClassId)
                        && cause.MethodId == Declare_MethodId
                        && cause.ReplyCode == NotFound;
        }

        public static bool IsMismatchedQueueArgs(Exception exception)
        {
            var cause = exception;
            ShutdownSignalException sig = null;
            while (cause != null && sig == null)
            {
                if (cause is ShutdownSignalException)
                {
                    sig = (ShutdownSignalException)cause;
                }

                cause = cause.InnerException;
            }

            if (sig == null)
            {
                return false;
            }
            else
            {
                return sig.ClassId == Queue_ClassId
               && sig.MethodId == Declare_MethodId
               && sig.ReplyCode == Precondition_Failed;
            }
        }
    }
}
