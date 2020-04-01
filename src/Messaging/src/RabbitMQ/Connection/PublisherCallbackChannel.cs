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
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Steeltoe.Messaging.Rabbit.Connection.CorrelationData;
using static Steeltoe.Messaging.Rabbit.Connection.IPublisherCallbackChannel;

namespace Steeltoe.Messaging.Rabbit.Connection
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class PublisherCallbackChannel : IPublisherCallbackChannel
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public const string RETURNED_MESSAGE_CORRELATION_KEY = "spring_returned_message_correlation";
        public const string RETURN_LISTENER_CORRELATION_KEY = "spring_listener_return_correlation";
        private readonly List<PendingConfirm> _emptyConfirms = new List<PendingConfirm>();
        private readonly IMessagePropertiesConverter _converter = new DefaultMessagePropertiesConverter();
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<IListener, SortedDictionary<ulong, PendingConfirm>> _pendingConfirms = new ConcurrentDictionary<IListener, SortedDictionary<ulong, PendingConfirm>>();
        private readonly ConcurrentDictionary<string, IListener> _listeners = new ConcurrentDictionary<string, IListener>();
        private readonly SortedDictionary<ulong, IListener> _listenerForSeq = new SortedDictionary<ulong, IListener>();
        private readonly ConcurrentDictionary<string, PendingConfirm> _pendingReturns = new ConcurrentDictionary<string, PendingConfirm>();
        private Action<IModel> _afterAckCallback;

        public PublisherCallbackChannel(IModel channel, ILogger logger = null)
        {
            Channel = channel;
            _logger = logger;
            channel.ModelShutdown += ShutdownCompleted;
        }

        #region IPublisherCallbackChannel

        public IModel Channel { get; }

        public IList<PendingConfirm> Expire(IPublisherCallbackChannel.IListener listener, long cutoffTime)
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

        public int GetPendingConfirmsCount(IPublisherCallbackChannel.IListener listener)
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

        public int GetPendingConfirmsCount()
        {
            lock (_lock)
            {
                return _pendingConfirms
                    .Values
                    .Select((p) => p.Count)
                    .Sum();
            }
        }

        public void AddListener(IListener listener)
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
                _logger?.LogDebug("Added listener " + listener);
            }
        }

        public void AddPendingConfirm(IPublisherCallbackChannel.IListener listener, ulong sequence, PendingConfirm pendingConfirm)
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

        public void SetAfterAckCallback(Action<IModel> callback)
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

        public int ChannelNumber => Channel.ChannelNumber;

        public ShutdownEventArgs CloseReason => Channel.CloseReason;

        public IBasicConsumer DefaultConsumer { get => Channel.DefaultConsumer; set => Channel.DefaultConsumer = value; }

        public bool IsClosed => Channel.IsClosed;

        public bool IsOpen => Channel.IsOpen;

        public ulong NextPublishSeqNo => Channel.NextPublishSeqNo;

        public TimeSpan ContinuationTimeout { get => Channel.ContinuationTimeout; set => Channel.ContinuationTimeout = value; }

        public event EventHandler<BasicAckEventArgs> BasicAcks
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

        public event EventHandler<BasicNackEventArgs> BasicNacks
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

        public event EventHandler<EventArgs> BasicRecoverOk
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

        public event EventHandler<BasicReturnEventArgs> BasicReturn
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

        public event EventHandler<CallbackExceptionEventArgs> CallbackException
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

        public event EventHandler<FlowControlEventArgs> FlowControl
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

        public event EventHandler<ShutdownEventArgs> ModelShutdown
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

        public void Abort() => Channel.Abort();

        public void Abort(ushort replyCode, string replyText) => Channel.Abort(replyCode, replyText);

        public void BasicAck(ulong deliveryTag, bool multiple) => Channel.BasicAck(deliveryTag, multiple);

        public void BasicCancel(string consumerTag) => Channel.BasicCancel(consumerTag);

        public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
            => Channel.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);

        public BasicGetResult BasicGet(string queue, bool autoAck) => Channel.BasicGet(queue, autoAck);

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue) => Channel.BasicNack(deliveryTag, multiple, requeue);

        public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body)
            => Channel.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global) => Channel.BasicQos(prefetchSize, prefetchCount, global);

        public void BasicRecover(bool requeue) => Channel.BasicRecover(requeue);

        public void BasicRecoverAsync(bool requeue) => Channel.BasicRecoverAsync(requeue);

        public void BasicReject(ulong deliveryTag, bool requeue) => Channel.BasicReject(deliveryTag, requeue);

        public void Close()
        {
            _logger?.LogDebug("Closing " + Channel);
            try
            {
                Channel.Close();
            }
            catch (AlreadyClosedException)
            {
                _logger?.LogTrace(Channel + " is already closed");
            }

            ShutdownCompleted("Channel closed by application");
        }

        public void Close(ushort replyCode, string replyText)
        {
            Channel.Close(replyCode, replyText);

            // if (this.delegate instanceof AutorecoveringChannel) {
            //    ClosingRecoveryListener.removeChannel((AutorecoveringChannel)this.delegate);
            // }
        }

        public void ConfirmSelect() => Channel.ConfirmSelect();

        public uint ConsumerCount(string queue) => Channel.ConsumerCount(queue);

        public IBasicProperties CreateBasicProperties() => Channel.CreateBasicProperties();

        public IBasicPublishBatch CreateBasicPublishBatch() => Channel.CreateBasicPublishBatch();

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
            => Channel.ExchangeBind(destination, source, routingKey, arguments);

        public void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
            => Channel.ExchangeBindNoWait(destination, source, routingKey, arguments);

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
            => Channel.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);

        public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
            => Channel.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);

        public void ExchangeDeclarePassive(string exchange) => Channel.ExchangeDeclarePassive(exchange);

        public void ExchangeDelete(string exchange, bool ifUnused) => Channel.ExchangeDelete(exchange, ifUnused);

        public void ExchangeDeleteNoWait(string exchange, bool ifUnused) => Channel.ExchangeDeleteNoWait(exchange, ifUnused);

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
            => Channel.ExchangeUnbind(destination, source, routingKey, arguments);

        public void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
            => Channel.ExchangeUnbindNoWait(destination, source, routingKey, arguments);

        public uint MessageCount(string queue) => Channel.MessageCount(queue);

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
            => Channel.QueueBind(queue, exchange, routingKey, arguments);

        public void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
            => Channel.QueueBindNoWait(queue, exchange, routingKey, arguments);

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
            => Channel.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);

        public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
            => Channel.QueueDeclareNoWait(queue, durable, exclusive, autoDelete, arguments);

        public QueueDeclareOk QueueDeclarePassive(string queue) => Channel.QueueDeclarePassive(queue);

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty) => Channel.QueueDelete(queue, ifUnused, ifEmpty);

        public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty) => Channel.QueueDeleteNoWait(queue, ifUnused, ifEmpty);

        public uint QueuePurge(string queue) => Channel.QueuePurge(queue);

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
            => Channel.QueueBind(queue, exchange, routingKey, arguments);

        public void TxCommit() => Channel.TxCommit();

        public void TxRollback() => Channel.TxRollback();

        public void TxSelect() => Channel.TxSelect();

        public bool WaitForConfirms() => Channel.WaitForConfirms();

        public bool WaitForConfirms(TimeSpan timeout) => Channel.WaitForConfirms(timeout);

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut) => Channel.WaitForConfirms(timeout, out timedOut);

        public void WaitForConfirmsOrDie() => Channel.WaitForConfirmsOrDie();

        public void WaitForConfirmsOrDie(TimeSpan timeout) => Channel.WaitForConfirmsOrDie(timeout);
        #endregion

        #region IDisposable Support
        public void Dispose()
        {
            // Do Nothing
        }
        #endregion

        #region Private
        private void ShutdownCompleted(object sender, ShutdownEventArgs e)
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
                        _logger?.LogDebug(ToString() + " PC:Nack:(close):" + confirmEntry.Key);
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
            if (properties.Headers.TryGetValue(RETURNED_MESSAGE_CORRELATION_KEY, out var returnCorrelation) && _pendingReturns.Remove(returnCorrelation.ToString(), out var confirm))
            {
                var messageProperties = _converter.ToMessageProperties(properties, new Envelope(0, false, args.Exchange, args.RoutingKey), System.Text.Encoding.UTF8);
                if (confirm.CorrelationInfo != null)
                {
                    confirm.CorrelationInfo.ReturnedMessage = new Message(args.Body, messageProperties);
                }
            }

            string uuidObject = null;
            if (properties.Headers.TryGetValue(RETURN_LISTENER_CORRELATION_KEY, out var returnListenerHeader))
            {
                uuidObject = returnListenerHeader.ToString();
            }

            IListener listener = null;
            if (uuidObject != null)
            {
                _listeners.TryGetValue(uuidObject, out listener);
            }
            else
            {
                _logger?.LogError("No '" + RETURN_LISTENER_CORRELATION_KEY + "' header in returned message");
            }

            if (listener == null || !listener.IsReturnListener)
            {
                _logger?.LogWarning("No Listener for returned message");
            }
            else
            {
                // _hasReturned = true;
                var listenerToInvoke = listener;

                // TODO: Not sure this needs its own thread .. probably does .. but need to explore Rabbit client threading model
                Task.Run(() =>
                {
                    try
                    {
                        listenerToInvoke.HandleReturn(args.ReplyCode, args.ReplyText, args.Exchange, args.RoutingKey, properties, args.Body);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError("Exception delivering returned message ", e);
                    }
                    finally
                    {
                        // TODO: Not sure this latch is needed ..
                        // I think its is to ensure the callback for return and confirm are not done at same time?
                        // Returns should happen first and then confirms
                        // _returnLatch.countDown();
                    }
                });
            }
        }

        private void HandleAck(object sender, BasicAckEventArgs args)
        {
            _logger?.LogDebug(ToString() + " PC:Ack:" + args.DeliveryTag + ":" + args.Multiple);
            ProcessAck(args.DeliveryTag, true, args.Multiple, true);
        }

        private void HandleNack(object sender, BasicNackEventArgs args)
        {
            _logger?.LogDebug(ToString() + " PC:Nack:" + args.DeliveryTag + ":" + args.Multiple);
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
                    _logger?.LogError("Failed to process publisher confirm", e);
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
                    _logger?.LogDebug(Channel.ToString() + " No listener for seq:" + seq);
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
                        _logger?.LogDebug("Sending confirm " + pendingConfirm);
                        listener.HandleConfirm(pendingConfirm, ack);
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError("Exception delivering confirm", e);
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
                        _logger?.LogError("Failed to invoke afterAckCallback", e);
                    }
                }
            });
        }

        #endregion
    }
}
