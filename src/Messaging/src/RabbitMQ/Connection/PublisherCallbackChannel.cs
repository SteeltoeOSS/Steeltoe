// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Steeltoe.Messaging.RabbitMQ.Connection.CorrelationData;
using static Steeltoe.Messaging.RabbitMQ.Connection.IPublisherCallbackChannel;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
public class PublisherCallbackChannel : IPublisherCallbackChannel
{
    public const string RETURNED_MESSAGE_CORRELATION_KEY = "spring_returned_message_correlation";
    public const string RETURN_LISTENER_CORRELATION_KEY = "spring_listener_return_correlation";
    public const string RETURN_LISTENER_ERROR = "No '" + RETURN_LISTENER_CORRELATION_KEY + "' header in returned message";
    private readonly List<PendingConfirm> _emptyConfirms = new ();
    private readonly IMessageHeadersConverter _converter = new DefaultMessageHeadersConverter();
    private readonly ILogger _logger;
    private readonly object _lock = new ();
    private readonly ConcurrentDictionary<IListener, SortedDictionary<ulong, PendingConfirm>> _pendingConfirms = new ();
    private readonly ConcurrentDictionary<string, IListener> _listeners = new ();
    private readonly SortedDictionary<ulong, IListener> _listenerForSeq = new ();
    private readonly ConcurrentDictionary<string, PendingConfirm> _pendingReturns = new ();
    private Action<RC.IModel> _afterAckCallback;

    public PublisherCallbackChannel(RC.IModel channel, ILogger logger = null)
    {
        Channel = channel;
        _logger = logger;
        channel.ModelShutdown += ShutdownCompleted;
    }

    #region IPublisherCallbackChannel

    public virtual RC.IModel Channel { get; }

    public virtual IList<PendingConfirm> Expire(IPublisherCallbackChannel.IListener listener, long cutoffTime)
    {
        lock (_lock)
        {
            if (!_pendingConfirms.TryGetValue(listener, out var pendingConfirmsForListener))
            {
                return _emptyConfirms;
            }
            else
            {
                var expired = new List<PendingConfirm>();
                var toRemove = new List<ulong>();
                foreach (var kvp in pendingConfirmsForListener)
                {
                    var pendingConfirm = kvp.Value;
                    if (pendingConfirm.Timestamp < cutoffTime)
                    {
                        expired.Add(pendingConfirm);
                        toRemove.Add(kvp.Key);
                        var correlationData = pendingConfirm.CorrelationInfo;
                        if (correlationData != null && !string.IsNullOrEmpty(correlationData.Id))
                        {
                            _pendingReturns.Remove(correlationData.Id, out var _); // NOSONAR never null
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var key in toRemove)
                {
                    pendingConfirmsForListener.Remove(key);
                }

                return expired;
            }
        }
    }

    public virtual int GetPendingConfirmsCount(IPublisherCallbackChannel.IListener listener)
    {
        lock (_lock)
        {
            if (!_pendingConfirms.TryGetValue(listener, out var pendingConfirmsForListener))
            {
                return 0;
            }
            else
            {
                return pendingConfirmsForListener.Count;
            }
        }
    }

    public virtual int GetPendingConfirmsCount()
    {
        lock (_lock)
        {
            return _pendingConfirms
                .Values
                .Select((p) => p.Count)
                .Sum();
        }
    }

    public virtual void AddListener(IListener listener)
    {
        if (listener == null)
        {
            throw new ArgumentNullException(nameof(listener));
        }

        if (_listeners.Count == 0)
        {
            Channel.BasicAcks += HandleAck;
            Channel.BasicNacks += HandleNack;
            Channel.BasicReturn += HandleReturn;
        }

        if (_listeners.TryAdd(listener.UUID, listener))
        {
            _pendingConfirms[listener] = new SortedDictionary<ulong, PendingConfirm>();
            _logger?.LogDebug("Added listener {listener}", listener);
        }
    }

    public virtual void AddPendingConfirm(IPublisherCallbackChannel.IListener listener, ulong sequence, PendingConfirm pendingConfirm)
    {
        lock (_lock)
        {
            if (!_pendingConfirms.TryGetValue(listener, out var pendingConfirmsForListener))
            {
                throw new ArgumentException(nameof(listener));
            }

            pendingConfirmsForListener[sequence] = pendingConfirm;
            _listenerForSeq[sequence] = listener;
            if (pendingConfirm.CorrelationInfo != null)
            {
                var returnCorrelation = pendingConfirm.CorrelationInfo.Id;
                if (!string.IsNullOrEmpty(returnCorrelation))
                {
                    _pendingReturns[returnCorrelation] = pendingConfirm;
                }
            }
        }
    }

    public virtual void SetAfterAckCallback(Action<RC.IModel> callback)
    {
        if (GetPendingConfirmsCount() == 0 && callback != null)
        {
            callback(this);
        }
        else
        {
            _afterAckCallback = callback;
        }
    }
    #endregion

    #region IModel

    public virtual int ChannelNumber => Channel.ChannelNumber;

    public virtual RC.ShutdownEventArgs CloseReason => Channel.CloseReason;

    public virtual RC.IBasicConsumer DefaultConsumer { get => Channel.DefaultConsumer; set => Channel.DefaultConsumer = value; }

    public virtual bool IsClosed => Channel.IsClosed;

    public virtual bool IsOpen => Channel.IsOpen;

    public virtual ulong NextPublishSeqNo => Channel.NextPublishSeqNo;

    public virtual TimeSpan ContinuationTimeout { get => Channel.ContinuationTimeout; set => Channel.ContinuationTimeout = value; }

    public virtual event EventHandler<BasicAckEventArgs> BasicAcks
    {
        add
        {
            Channel.BasicAcks += value;
        }

        remove
        {
            Channel.BasicAcks -= value;
        }
    }

    public virtual event EventHandler<BasicNackEventArgs> BasicNacks
    {
        add
        {
            Channel.BasicNacks += value;
        }

        remove
        {
            Channel.BasicNacks -= value;
        }
    }

    public virtual event EventHandler<EventArgs> BasicRecoverOk
    {
        add
        {
            Channel.BasicRecoverOk += value;
        }

        remove
        {
            Channel.BasicRecoverOk -= value;
        }
    }

    public virtual event EventHandler<BasicReturnEventArgs> BasicReturn
    {
        add
        {
            Channel.BasicReturn += value;
        }

        remove
        {
            Channel.BasicReturn -= value;
        }
    }

    public virtual event EventHandler<CallbackExceptionEventArgs> CallbackException
    {
        add
        {
            Channel.CallbackException += value;
        }

        remove
        {
            Channel.CallbackException -= value;
        }
    }

    public virtual event EventHandler<FlowControlEventArgs> FlowControl
    {
        add
        {
            Channel.FlowControl += value;
        }

        remove
        {
            Channel.FlowControl -= value;
        }
    }

    public virtual event EventHandler<RC.ShutdownEventArgs> ModelShutdown
    {
        add
        {
            Channel.ModelShutdown += value;
        }

        remove
        {
            Channel.ModelShutdown -= value;
        }
    }

    public virtual void Abort() => Channel.Abort();

    public virtual void Abort(ushort replyCode, string replyText) => Channel.Abort(replyCode, replyText);

    public virtual void BasicAck(ulong deliveryTag, bool multiple) => Channel.BasicAck(deliveryTag, multiple);

    public virtual void BasicCancel(string consumerTag) => Channel.BasicCancel(consumerTag);

    public virtual string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, RC.IBasicConsumer consumer)
        => Channel.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);

    public virtual RC.BasicGetResult BasicGet(string queue, bool autoAck) => Channel.BasicGet(queue, autoAck);

    public virtual void BasicNack(ulong deliveryTag, bool multiple, bool requeue) => Channel.BasicNack(deliveryTag, multiple, requeue);

    public virtual void BasicPublish(string exchange, string routingKey, bool mandatory, RC.IBasicProperties basicProperties, byte[] body)
        => Channel.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);

    public virtual void BasicQos(uint prefetchSize, ushort prefetchCount, bool global) => Channel.BasicQos(prefetchSize, prefetchCount, global);

    public virtual void BasicRecover(bool requeue) => Channel.BasicRecover(requeue);

    public virtual void BasicRecoverAsync(bool requeue) => Channel.BasicRecoverAsync(requeue);

    public virtual void BasicReject(ulong deliveryTag, bool requeue) => Channel.BasicReject(deliveryTag, requeue);

    public virtual void Close()
    {
        _logger?.LogDebug("Closing channel {channel}", Channel);
        try
        {
            Channel.Close();
        }
        catch (AlreadyClosedException e)
        {
            _logger?.LogTrace(e, "Channel {channel} is already closed", Channel);
        }

        ShutdownCompleted("Channel closed by application");
    }

    public virtual void Close(ushort replyCode, string replyText)
    {
        Channel.Close(replyCode, replyText);
    }

    public virtual void ConfirmSelect() => Channel.ConfirmSelect();

    public virtual uint ConsumerCount(string queue) => Channel.ConsumerCount(queue);

    public virtual RC.IBasicProperties CreateBasicProperties() => Channel.CreateBasicProperties();

    public virtual RC.IBasicPublishBatch CreateBasicPublishBatch() => Channel.CreateBasicPublishBatch();

    public virtual void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        => Channel.ExchangeBind(destination, source, routingKey, arguments);

    public virtual void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        => Channel.ExchangeBindNoWait(destination, source, routingKey, arguments);

    public virtual void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        => Channel.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);

    public virtual void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        => Channel.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);

    public virtual void ExchangeDeclarePassive(string exchange) => Channel.ExchangeDeclarePassive(exchange);

    public virtual void ExchangeDelete(string exchange, bool ifUnused) => Channel.ExchangeDelete(exchange, ifUnused);

    public virtual void ExchangeDeleteNoWait(string exchange, bool ifUnused) => Channel.ExchangeDeleteNoWait(exchange, ifUnused);

    public virtual void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        => Channel.ExchangeUnbind(destination, source, routingKey, arguments);

    public virtual void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        => Channel.ExchangeUnbindNoWait(destination, source, routingKey, arguments);

    public virtual uint MessageCount(string queue) => Channel.MessageCount(queue);

    public virtual void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        => Channel.QueueBind(queue, exchange, routingKey, arguments);

    public virtual void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        => Channel.QueueBindNoWait(queue, exchange, routingKey, arguments);

    public virtual RC.QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        => Channel.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);

    public virtual void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        => Channel.QueueDeclareNoWait(queue, durable, exclusive, autoDelete, arguments);

    public virtual RC.QueueDeclareOk QueueDeclarePassive(string queue) => Channel.QueueDeclarePassive(queue);

    public virtual uint QueueDelete(string queue, bool ifUnused, bool ifEmpty) => Channel.QueueDelete(queue, ifUnused, ifEmpty);

    public virtual void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty) => Channel.QueueDeleteNoWait(queue, ifUnused, ifEmpty);

    public virtual uint QueuePurge(string queue) => Channel.QueuePurge(queue);

    public virtual void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        => Channel.QueueBind(queue, exchange, routingKey, arguments);

    public virtual void TxCommit() => Channel.TxCommit();

    public virtual void TxRollback() => Channel.TxRollback();

    public virtual void TxSelect() => Channel.TxSelect();

    public virtual bool WaitForConfirms() => Channel.WaitForConfirms();

    public virtual bool WaitForConfirms(TimeSpan timeout) => Channel.WaitForConfirms(timeout);

    public virtual bool WaitForConfirms(TimeSpan timeout, out bool timedOut) => Channel.WaitForConfirms(timeout, out timedOut);

    public virtual void WaitForConfirmsOrDie() => Channel.WaitForConfirmsOrDie();

    public virtual void WaitForConfirmsOrDie(TimeSpan timeout) => Channel.WaitForConfirmsOrDie(timeout);
    #endregion

    #region IDisposable Support
    public void Dispose()
    {
        // Do Nothing
    }
    #endregion

    #region Private
    private void ShutdownCompleted(object sender, RC.ShutdownEventArgs e)
    {
        ShutdownCompleted(e.ReplyText);
    }

    private void ShutdownCompleted(string cause)
    {
        GenerateNacksForPendingAcks(cause);
    }

    private void GenerateNacksForPendingAcks(string cause)
    {
        lock (_lock)
        {
            foreach (var entry in _pendingConfirms)
            {
                var listener = entry.Key;
                foreach (var confirmEntry in entry.Value)
                {
                    confirmEntry.Value.Cause = cause;
                    _logger?.LogDebug("{channel} PC:Nack:(close):{confirmEntry}", ToString(), confirmEntry.Key);
                    ProcessAck(confirmEntry.Key, false, false, false);
                }

                listener.Revoke(this);
            }

            _logger?.LogDebug("PendingConfirms cleared");
            _pendingConfirms.Clear();
            _listenerForSeq.Clear();
            _listeners.Clear();
        }
    }

    private void HandleReturn(object sender, BasicReturnEventArgs args)
    {
        var properties = args.BasicProperties;
        var messageProperties = _converter.ToMessageHeaders(properties, new Envelope(0, false, args.Exchange, args.RoutingKey), EncodingUtils.GetDefaultEncoding());
        if (properties.Headers.TryGetValue(RETURNED_MESSAGE_CORRELATION_KEY, out var returnCorrelation) && _pendingReturns.Remove(returnCorrelation.ToString(), out var confirm) && confirm.CorrelationInfo != null)
        {
            confirm.CorrelationInfo.ReturnedMessage = Message.Create(args.Body, messageProperties);
        }

        var uuidObject = messageProperties.Get<string>(RETURN_LISTENER_CORRELATION_KEY);

        IListener listener = null;
        if (uuidObject != null)
        {
            _listeners.TryGetValue(uuidObject, out listener);
        }
        else
        {
            _logger?.LogError(RETURN_LISTENER_ERROR);
        }

        if (listener == null || !listener.IsReturnListener)
        {
            _logger?.LogWarning("No Listener for returned message");
        }
        else
        {
            // _hasReturned = true;
            var listenerToInvoke = listener;

            try
            {
                listenerToInvoke.HandleReturn(args.ReplyCode, args.ReplyText, args.Exchange, args.RoutingKey, properties, args.Body);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception delivering returned message ");
            }
            finally
            {
                // TODO: Not sure this latch is needed ..
                // I think its is to ensure the callback for return and confirm are not done at same time?
                // Returns should happen first and then confirms
                // _returnLatch.countDown();
            }
        }
    }

    private void HandleAck(object sender, BasicAckEventArgs args)
    {
        _logger?.LogDebug("{channel} PC:Ack: {deliveryTag}:{multiple}", ToString(), args.DeliveryTag, args.Multiple);
        ProcessAck(args.DeliveryTag, true, args.Multiple, true);
    }

    private void HandleNack(object sender, BasicNackEventArgs args)
    {
        _logger?.LogDebug("{channel} PC:Nack: {deliveryTag}:{multiple}", ToString(), args.DeliveryTag, args.Multiple);
        ProcessAck(args.DeliveryTag, false, args.Multiple, true);
    }

    private void ProcessAck(ulong seq, bool ack, bool multiple, bool remove)
    {
        lock (_lock)
        {
            try
            {
                DoProcessAck(seq, ack, multiple, remove);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to process publisher confirm");
            }
        }
    }

    private void DoProcessAck(ulong seq, bool ack, bool multiple, bool remove)
    {
        if (multiple)
        {
            ProcessMultipleAck(seq, ack);
        }
        else
        {
            if (_listenerForSeq.TryGetValue(seq, out var listener))
            {
                PendingConfirm pendingConfirm = null;
                if (_pendingConfirms.TryGetValue(listener, out var confirmsForListener))
                {
                    if (remove)
                    {
                        confirmsForListener.Remove(seq, out pendingConfirm);
                    }
                    else
                    {
                        confirmsForListener.TryGetValue(seq, out pendingConfirm);
                    }
                }

                if (pendingConfirm != null)
                {
                    var correlationData = pendingConfirm.CorrelationInfo;
                    if (correlationData != null)
                    {
                        correlationData.FutureSource.SetResult(new Confirm(ack, pendingConfirm.Cause));
                        if (!string.IsNullOrEmpty(correlationData.Id))
                        {
                            _pendingReturns.Remove(correlationData.Id, out var removedConfirm);
                        }
                    }

                    DoHandleConfirm(ack, listener, pendingConfirm);
                }
            }
            else
            {
                _logger?.LogDebug("{channel} No listener for seq: {seq}", Channel, seq);
            }
        }
    }

    private void ProcessMultipleAck(ulong seq, bool ack)
    {
        // Piggy-backed ack - extract all Listeners for this and earlier
        // sequences. Then, for each Listener, handle each of it's acks.
        // Finally, remove the sequences from listenerForSeq.
        var involvedListeners = _listenerForSeq.Where((kvp) => kvp.Key < seq + 1);

        // eliminate duplicates
        var listenersForAcks = involvedListeners.Select((kvp) => kvp.Value).Distinct();

        // Set<Listener> listenersForAcks = new HashSet<IListener>(involvedListeners.values());
        foreach (var involvedListener in listenersForAcks)
        {
            // find all unack'd confirms for this listener and handle them
            if (_pendingConfirms.TryGetValue(involvedListener, out var confirmsMap))
            {
                var confirms = confirmsMap.Where((kvp) => kvp.Key < seq + 1);

                // Iterator<Entry<Long, PendingConfirm>> iterator = confirms.entrySet().iterator();
                // while (iterator.hasNext())
                // {
                foreach (var entry in confirms)
                {
                    // Entry<Long, PendingConfirm> entry = iterator.next();
                    var value = entry.Value;
                    var correlationData = value.CorrelationInfo;
                    if (correlationData != null)
                    {
                        correlationData.FutureSource.SetResult(new Confirm(ack, value.Cause));
                        if (!string.IsNullOrEmpty(correlationData.Id))
                        {
                            _pendingReturns.Remove(correlationData.Id, out var removedConfirm);
                        }
                    }

                    // iterator.remove();
                    DoHandleConfirm(ack, involvedListener, value);
                }
            }
        }

        var seqs = new List<ulong>(involvedListeners.Select((kvp) => kvp.Key));
        foreach (var key in seqs)
        {
            _listenerForSeq.Remove(key);
        }
    }

    private void DoHandleConfirm(bool ack, IListener listener, PendingConfirm pendingConfirm)
    {
        Task.Run(() =>
        {
            try
            {
                if (listener.IsConfirmListener)
                {
                    // TODO: Not sure this latch is needed ..
                    // I think its is to ensure the callback for return and confirm are not done at same time?
                    // Returns should happen first and then confirms

                    // if (this.hasReturned && !this.returnLatch.await(RETURN_CALLBACK_TIMEOUT, TimeUnit.SECONDS))
                    // {
                    //    this.logger
                    //            .error("Return callback failed to execute in " + RETURN_CALLBACK_TIMEOUT + " seconds");
                    // }
                    _logger?.LogDebug("Sending confirm {confirm} ", pendingConfirm);
                    listener.HandleConfirm(pendingConfirm, ack);
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception delivering confirm");
            }
            finally
            {
                try
                {
                    if (_afterAckCallback != null && GetPendingConfirmsCount() == 0)
                    {
                        _afterAckCallback(this);
                        _afterAckCallback = null;
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Failed to invoke afterAckCallback");
                }
            }
        });
    }

    #endregion
}
#pragma warning restore S3881 // "IDisposable" should be implemented correctly