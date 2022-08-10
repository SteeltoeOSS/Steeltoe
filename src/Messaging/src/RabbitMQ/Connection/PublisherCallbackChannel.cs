// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support;
using static Steeltoe.Messaging.RabbitMQ.Connection.CorrelationData;
using static Steeltoe.Messaging.RabbitMQ.Connection.IPublisherCallbackChannel;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class PublisherCallbackChannel : IPublisherCallbackChannel
{
    public const string ReturnedMessageCorrelationKey = "spring_returned_message_correlation";
    public const string ReturnListenerCorrelationKey = "spring_listener_return_correlation";
    public const string ReturnListenerError = $"No '{ReturnListenerCorrelationKey}' header in returned message";
    private readonly List<PendingConfirm> _emptyConfirms = new();
    private readonly IMessageHeadersConverter _converter = new DefaultMessageHeadersConverter();
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<IListener, SortedDictionary<ulong, PendingConfirm>> _pendingConfirms = new();
    private readonly ConcurrentDictionary<string, IListener> _listeners = new();
    private readonly SortedDictionary<ulong, IListener> _listenerForSeq = new();
    private readonly ConcurrentDictionary<string, PendingConfirm> _pendingReturns = new();
    private Action<RC.IModel> _afterAckCallback;

    public virtual RC.IModel Channel { get; }

    public virtual int ChannelNumber => Channel.ChannelNumber;

    public virtual RC.ShutdownEventArgs CloseReason => Channel.CloseReason;

    public virtual RC.IBasicConsumer DefaultConsumer
    {
        get => Channel.DefaultConsumer;
        set => Channel.DefaultConsumer = value;
    }

    public virtual bool IsClosed => Channel.IsClosed;

    public virtual bool IsOpen => Channel.IsOpen;

    public virtual ulong NextPublishSeqNo => Channel.NextPublishSeqNo;

    public virtual TimeSpan ContinuationTimeout
    {
        get => Channel.ContinuationTimeout;
        set => Channel.ContinuationTimeout = value;
    }

    public virtual event EventHandler<BasicAckEventArgs> BasicAcks
    {
        add => Channel.BasicAcks += value;

        remove => Channel.BasicAcks -= value;
    }

    public virtual event EventHandler<BasicNackEventArgs> BasicNacks
    {
        add => Channel.BasicNacks += value;

        remove => Channel.BasicNacks -= value;
    }

    public virtual event EventHandler<EventArgs> BasicRecoverOk
    {
        add => Channel.BasicRecoverOk += value;

        remove => Channel.BasicRecoverOk -= value;
    }

    public virtual event EventHandler<BasicReturnEventArgs> BasicReturn
    {
        add => Channel.BasicReturn += value;

        remove => Channel.BasicReturn -= value;
    }

    public virtual event EventHandler<CallbackExceptionEventArgs> CallbackException
    {
        add => Channel.CallbackException += value;

        remove => Channel.CallbackException -= value;
    }

    public virtual event EventHandler<FlowControlEventArgs> FlowControl
    {
        add => Channel.FlowControl += value;

        remove => Channel.FlowControl -= value;
    }

    public virtual event EventHandler<RC.ShutdownEventArgs> ModelShutdown
    {
        add => Channel.ModelShutdown += value;

        remove => Channel.ModelShutdown -= value;
    }

    public PublisherCallbackChannel(RC.IModel channel, ILogger logger = null)
    {
        Channel = channel;
        _logger = logger;
        channel.ModelShutdown += ShutdownCompleted;
    }

    public virtual IList<PendingConfirm> Expire(IListener listener, long cutoffTime)
    {
        lock (_lock)
        {
            if (!_pendingConfirms.TryGetValue(listener, out SortedDictionary<ulong, PendingConfirm> pendingConfirmsForListener))
            {
                return _emptyConfirms;
            }

            var expired = new List<PendingConfirm>();
            var toRemove = new List<ulong>();

            foreach (KeyValuePair<ulong, PendingConfirm> kvp in pendingConfirmsForListener)
            {
                PendingConfirm pendingConfirm = kvp.Value;

                if (pendingConfirm.Timestamp < cutoffTime)
                {
                    expired.Add(pendingConfirm);
                    toRemove.Add(kvp.Key);
                    CorrelationData correlationData = pendingConfirm.CorrelationInfo;

                    if (correlationData != null && !string.IsNullOrEmpty(correlationData.Id))
                    {
                        _pendingReturns.Remove(correlationData.Id, out PendingConfirm _);
                    }
                }
                else
                {
                    break;
                }
            }

            foreach (ulong key in toRemove)
            {
                pendingConfirmsForListener.Remove(key);
            }

            return expired;
        }
    }

    public virtual int GetPendingConfirmsCount(IListener listener)
    {
        lock (_lock)
        {
            if (!_pendingConfirms.TryGetValue(listener, out SortedDictionary<ulong, PendingConfirm> pendingConfirmsForListener))
            {
                return 0;
            }

            return pendingConfirmsForListener.Count;
        }
    }

    public virtual int GetPendingConfirmsCount()
    {
        lock (_lock)
        {
            return _pendingConfirms.Values.Select(p => p.Count).Sum();
        }
    }

    public virtual void AddListener(IListener listener)
    {
        ArgumentGuard.NotNull(listener);

        if (_listeners.Count == 0)
        {
            Channel.BasicAcks += HandleAck;
            Channel.BasicNacks += HandleNack;
            Channel.BasicReturn += HandleReturn;
        }

        if (_listeners.TryAdd(listener.Uuid, listener))
        {
            _pendingConfirms[listener] = new SortedDictionary<ulong, PendingConfirm>();
            _logger?.LogDebug("Added listener {listener}", listener);
        }
    }

    public virtual void AddPendingConfirm(IListener listener, ulong sequence, PendingConfirm pendingConfirm)
    {
        lock (_lock)
        {
            if (!_pendingConfirms.TryGetValue(listener, out SortedDictionary<ulong, PendingConfirm> pendingConfirmsForListener))
            {
                throw new ArgumentException("Listener not found in pending confirms.", nameof(listener));
            }

            pendingConfirmsForListener[sequence] = pendingConfirm;
            _listenerForSeq[sequence] = listener;

            if (pendingConfirm.CorrelationInfo != null)
            {
                string returnCorrelation = pendingConfirm.CorrelationInfo.Id;

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

    public virtual void Abort()
    {
        Channel.Abort();
    }

    public virtual void Abort(ushort replyCode, string replyText)
    {
        Channel.Abort(replyCode, replyText);
    }

    public virtual void BasicAck(ulong deliveryTag, bool multiple)
    {
        Channel.BasicAck(deliveryTag, multiple);
    }

    public virtual void BasicCancel(string consumerTag)
    {
        Channel.BasicCancel(consumerTag);
    }

    public virtual string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments,
        RC.IBasicConsumer consumer)
    {
        return Channel.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);
    }

    public virtual RC.BasicGetResult BasicGet(string queue, bool autoAck)
    {
        return Channel.BasicGet(queue, autoAck);
    }

    public virtual void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
    {
        Channel.BasicNack(deliveryTag, multiple, requeue);
    }

    public virtual void BasicPublish(string exchange, string routingKey, bool mandatory, RC.IBasicProperties basicProperties, byte[] body)
    {
        Channel.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
    }

    public virtual void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
    {
        Channel.BasicQos(prefetchSize, prefetchCount, global);
    }

    public virtual void BasicRecover(bool requeue)
    {
        Channel.BasicRecover(requeue);
    }

    public virtual void BasicRecoverAsync(bool requeue)
    {
        Channel.BasicRecoverAsync(requeue);
    }

    public virtual void BasicReject(ulong deliveryTag, bool requeue)
    {
        Channel.BasicReject(deliveryTag, requeue);
    }

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

    public virtual void ConfirmSelect()
    {
        Channel.ConfirmSelect();
    }

    public virtual uint ConsumerCount(string queue)
    {
        return Channel.ConsumerCount(queue);
    }

    public virtual RC.IBasicProperties CreateBasicProperties()
    {
        return Channel.CreateBasicProperties();
    }

    public virtual RC.IBasicPublishBatch CreateBasicPublishBatch()
    {
        return Channel.CreateBasicPublishBatch();
    }

    public virtual void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        Channel.ExchangeBind(destination, source, routingKey, arguments);
    }

    public virtual void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        Channel.ExchangeBindNoWait(destination, source, routingKey, arguments);
    }

    public virtual void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
    {
        Channel.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
    }

    public virtual void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
    {
        Channel.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);
    }

    public virtual void ExchangeDeclarePassive(string exchange)
    {
        Channel.ExchangeDeclarePassive(exchange);
    }

    public virtual void ExchangeDelete(string exchange, bool ifUnused)
    {
        Channel.ExchangeDelete(exchange, ifUnused);
    }

    public virtual void ExchangeDeleteNoWait(string exchange, bool ifUnused)
    {
        Channel.ExchangeDeleteNoWait(exchange, ifUnused);
    }

    public virtual void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        Channel.ExchangeUnbind(destination, source, routingKey, arguments);
    }

    public virtual void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
    {
        Channel.ExchangeUnbindNoWait(destination, source, routingKey, arguments);
    }

    public virtual uint MessageCount(string queue)
    {
        return Channel.MessageCount(queue);
    }

    public virtual void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
    {
        Channel.QueueBind(queue, exchange, routingKey, arguments);
    }

    public virtual void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
    {
        Channel.QueueBindNoWait(queue, exchange, routingKey, arguments);
    }

    public virtual RC.QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
    {
        return Channel.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
    }

    public virtual void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
    {
        Channel.QueueDeclareNoWait(queue, durable, exclusive, autoDelete, arguments);
    }

    public virtual RC.QueueDeclareOk QueueDeclarePassive(string queue)
    {
        return Channel.QueueDeclarePassive(queue);
    }

    public virtual uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
    {
        return Channel.QueueDelete(queue, ifUnused, ifEmpty);
    }

    public virtual void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty)
    {
        Channel.QueueDeleteNoWait(queue, ifUnused, ifEmpty);
    }

    public virtual uint QueuePurge(string queue)
    {
        return Channel.QueuePurge(queue);
    }

    public virtual void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
    {
        Channel.QueueBind(queue, exchange, routingKey, arguments);
    }

    public virtual void TxCommit()
    {
        Channel.TxCommit();
    }

    public virtual void TxRollback()
    {
        Channel.TxRollback();
    }

    public virtual void TxSelect()
    {
        Channel.TxSelect();
    }

    public virtual bool WaitForConfirms()
    {
        return Channel.WaitForConfirms();
    }

    public virtual bool WaitForConfirms(TimeSpan timeout)
    {
        return Channel.WaitForConfirms(timeout);
    }

    public virtual bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
    {
        return Channel.WaitForConfirms(timeout, out timedOut);
    }

    public virtual void WaitForConfirmsOrDie()
    {
        Channel.WaitForConfirmsOrDie();
    }

    public virtual void WaitForConfirmsOrDie(TimeSpan timeout)
    {
        Channel.WaitForConfirmsOrDie(timeout);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

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
            foreach (KeyValuePair<IListener, SortedDictionary<ulong, PendingConfirm>> entry in _pendingConfirms)
            {
                IListener listener = entry.Key;

                foreach (KeyValuePair<ulong, PendingConfirm> confirmEntry in entry.Value)
                {
                    confirmEntry.Value.Cause = cause;
                    _logger?.LogDebug("{channel} PC:Nack:(close):{confirmEntry}", this, confirmEntry.Key);
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
        RC.IBasicProperties properties = args.BasicProperties;

        IMessageHeaders messageProperties =
            _converter.ToMessageHeaders(properties, new Envelope(0, false, args.Exchange, args.RoutingKey), EncodingUtils.GetDefaultEncoding());

        if (properties.Headers.TryGetValue(ReturnedMessageCorrelationKey, out object returnCorrelation) &&
            _pendingReturns.Remove(returnCorrelation.ToString(), out PendingConfirm confirm) && confirm.CorrelationInfo != null)
        {
            confirm.CorrelationInfo.ReturnedMessage = Message.Create(args.Body, messageProperties);
        }

        string uuidObject = messageProperties.Get<string>(ReturnListenerCorrelationKey);

        IListener listener = null;

        if (uuidObject != null)
        {
            _listeners.TryGetValue(uuidObject, out listener);
        }
        else
        {
            _logger?.LogError(ReturnListenerError);
        }

        if (listener == null || !listener.IsReturnListener)
        {
            _logger?.LogWarning("No Listener for returned message");
        }
        else
        {
            // _hasReturned = true;
            IListener listenerToInvoke = listener;

            try
            {
                listenerToInvoke.HandleReturn(args.ReplyCode, args.ReplyText, args.Exchange, args.RoutingKey, properties, args.Body);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception delivering returned message ");
            }
        }
    }

    private void HandleAck(object sender, BasicAckEventArgs args)
    {
        _logger?.LogDebug("{channel} PC:Ack: {deliveryTag}:{multiple}", this, args.DeliveryTag, args.Multiple);
        ProcessAck(args.DeliveryTag, true, args.Multiple, true);
    }

    private void HandleNack(object sender, BasicNackEventArgs args)
    {
        _logger?.LogDebug("{channel} PC:Nack: {deliveryTag}:{multiple}", this, args.DeliveryTag, args.Multiple);
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
            if (_listenerForSeq.TryGetValue(seq, out IListener listener))
            {
                PendingConfirm pendingConfirm = null;

                if (_pendingConfirms.TryGetValue(listener, out SortedDictionary<ulong, PendingConfirm> confirmsForListener))
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
                    CorrelationData correlationData = pendingConfirm.CorrelationInfo;

                    if (correlationData != null)
                    {
                        correlationData.FutureSource.SetResult(new Confirm(ack, pendingConfirm.Cause));

                        if (!string.IsNullOrEmpty(correlationData.Id))
                        {
                            _pendingReturns.Remove(correlationData.Id, out _);
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
        IEnumerable<KeyValuePair<ulong, IListener>> involvedListeners = _listenerForSeq.Where(kvp => kvp.Key < seq + 1);

        // eliminate duplicates
        IEnumerable<IListener> listenersForAcks = involvedListeners.Select(kvp => kvp.Value).Distinct();

        // Set<Listener> listenersForAcks = new HashSet<IListener>(involvedListeners.values());
        foreach (IListener involvedListener in listenersForAcks)
        {
            // find all unack'd confirms for this listener and handle them
            if (_pendingConfirms.TryGetValue(involvedListener, out SortedDictionary<ulong, PendingConfirm> confirmsMap))
            {
                IEnumerable<KeyValuePair<ulong, PendingConfirm>> confirms = confirmsMap.Where(kvp => kvp.Key < seq + 1);

                // Iterator<Entry<Long, PendingConfirm>> iterator = confirms.entrySet().iterator();
                // while (iterator.hasNext())
                // {
                foreach (KeyValuePair<ulong, PendingConfirm> entry in confirms)
                {
                    // Entry<Long, PendingConfirm> entry = iterator.next();
                    PendingConfirm value = entry.Value;
                    CorrelationData correlationData = value.CorrelationInfo;

                    if (correlationData != null)
                    {
                        correlationData.FutureSource.SetResult(new Confirm(ack, value.Cause));

                        if (!string.IsNullOrEmpty(correlationData.Id))
                        {
                            _pendingReturns.Remove(correlationData.Id, out _);
                        }
                    }

                    // iterator.remove();
                    DoHandleConfirm(ack, involvedListener, value);
                }
            }
        }

        var sequence = new List<ulong>(involvedListeners.Select(kvp => kvp.Key));

        foreach (ulong key in sequence)
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
                    _logger?.LogDebug("Sending confirm {confirm}", pendingConfirm);
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
}
