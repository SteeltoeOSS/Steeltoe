// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Impl;
using Steeltoe.Common;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public static class RabbitUtils
{
    public const int ReplySuccess = 200;
    public const int NotFound = 404;
    public const int PreconditionFailed = 406;
    public const int CommandInvalid = 503;

    public const ushort ExchangeClassId = 40;
    public const ushort QueueClassId = 50;
    public const ushort DeclareMethodId = 10;
    public const ushort ChannelCloseClassId = 20;
    public const ushort ChannelCloseMethodId = 40;
    public const ushort ConnectionCloseClassId = 10;
    public const ushort ConnectionCloseMethodId = 50;

    private static readonly AsyncLocal<bool?> PhysicalCloseRequired = new();

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
                logger?.LogDebug(ex, "Ignoring Connection exception - assuming already closed");
            }
        }
    }

    public static void CloseChannel(RC.IModel channel, ILogger logger = null)
    {
        if (channel != null)
        {
            try
            {
                channel.Close();
            }
            catch (AlreadyClosedException)
            {
                // empty
            }
            catch (ShutdownSignalException sig)
            {
                if (!IsNormalShutdown(sig.Args))
                {
                    logger?.LogDebug(sig, "Unexpected exception on closing RabbitMQ Channel");
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Unexpected exception on closing RabbitMQ Channel");
            }
        }
    }

    public static void CommitIfNecessary(RC.IModel channel, ILogger logger = null)
    {
        ArgumentGuard.NotNull(channel);

        try
        {
            channel.TxCommit();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during TxCommit");
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
        }
    }

    public static void RollbackIfNecessary(RC.IModel channel, ILogger logger = null)
    {
        ArgumentGuard.NotNull(channel);

        try
        {
            channel.TxRollback();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during TxCommit");
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
        }
    }

    public static void CloseMessageConsumer(RC.IModel channel, IEnumerable<string> consumerTags, bool transactional, ILogger logger = null)
    {
        if (!channel.IsOpen)
        {
            return;
        }

        try
        {
            foreach (string consumerTag in consumerTags)
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
            logger?.LogError(ex, "Exception during CloseMessageConsumer");

            throw RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
        }
    }

    public static void Cancel(RC.IModel channel, string consumerTag, ILogger logger = null)
    {
        try
        {
            channel.BasicCancel(consumerTag);
        }
        catch (AlreadyClosedException e)
        {
            logger?.LogTrace(e, "{channel} is already closed", channel);
        }
        catch (Exception e)
        {
            logger?.LogDebug(e, "Error performing 'basicCancel' on {channel}", channel);
        }
    }

    public static void DeclareTransactional(RC.IModel channel, ILogger logger = null)
    {
        try
        {
            channel.TxSelect();
        }
        catch (IOException e)
        {
            logger?.LogDebug(e, "Error performing 'txSelect' on {channel}", channel);
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
        }
    }

    public static void SetPhysicalCloseRequired(RC.IModel channel, bool b)
    {
        if (channel is IChannelProxy)
        {
            PhysicalCloseRequired.Value = b;
        }
    }

    public static bool IsPhysicalCloseRequired()
    {
        bool? mustClose = PhysicalCloseRequired.Value;

        if (mustClose == null)
        {
            mustClose = false;
        }
        else
        {
            PhysicalCloseRequired.Value = null;
        }

        return mustClose.Value;
    }

    public static bool IsNormalChannelClose(RC.ShutdownEventArgs args)
    {
        if (IsNormalShutdown(args))
        {
            return true;
        }

        if (args.ClassId == ChannelCloseClassId && args.MethodId == ChannelCloseMethodId && args.ReplyCode == ReplySuccess && args.ReplyText == "OK")
        {
            return true;
        }

        return args.Initiator == RC.ShutdownInitiator.Application && args.ClassId == 0 && args.MethodId == 0 && args.ReplyText == "Goodbye";
    }

    public static bool IsNormalShutdown(RC.ShutdownEventArgs args)
    {
        if (args.ClassId == ConnectionCloseClassId && args.MethodId == ConnectionCloseMethodId && args.ReplyCode == ReplySuccess && args.ReplyText == "OK")
        {
            return true;
        }

        return args.Initiator == RC.ShutdownInitiator.Application && args.ClassId == 0 && args.MethodId == 0 && args.ReplyText == "Goodbye";
    }

    public static bool IsPassiveDeclarationChannelClose(Exception exception)
    {
        RC.ShutdownEventArgs cause = null;

        switch (exception)
        {
            case ShutdownSignalException shutdownSignalException:
                cause = shutdownSignalException.Args;
                break;
            case ProtocolException protocolException:
                cause = protocolException.ShutdownReason;
                break;
        }

        if (cause != null)
        {
            return (cause.ClassId == ExchangeClassId || cause.ClassId == QueueClassId) && cause.MethodId == DeclareMethodId && cause.ReplyCode == NotFound;
        }

        return false;
    }

    public static bool IsMismatchedQueueArgs(Exception exception)
    {
        Exception cause = exception;
        RC.ShutdownEventArgs args = null;

        while (cause != null && args == null)
        {
            if (cause is ShutdownSignalException exception1)
            {
                args = exception1.Args;
            }

            if (cause is OperationInterruptedException exception2)
            {
                args = exception2.ShutdownReason;
            }

            cause = cause.InnerException;
        }

        if (args == null)
        {
            return false;
        }

        return IsMismatchedQueueArgs(args);
    }

    public static bool IsMismatchedQueueArgs(RC.ShutdownEventArgs args)
    {
        return args.ClassId == QueueClassId && args.MethodId == DeclareMethodId && args.ReplyCode == PreconditionFailed;
    }

    public static int GetMaxFrame(IConnectionFactory connectionFactory)
    {
        try
        {
            return (int)connectionFactory.CreateConnection().Connection.FrameMax;
        }
        catch (Exception)
        {
            // Ignore
        }

        return -1;
    }

    internal static bool IsExchangeDeclarationFailure(RabbitIOException e)
    {
        Exception cause = e;
        RC.ShutdownEventArgs args = null;

        while (cause != null && args == null)
        {
            if (cause is OperationInterruptedException exception)
            {
                args = exception.ShutdownReason;
            }

            cause = cause.InnerException;
        }

        if (args == null)
        {
            return false;
        }

        return args.ClassId == ExchangeClassId && args.MethodId == DeclareMethodId && args.ReplyCode == CommandInvalid;
    }
}
