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

using RabbitMQ.Client;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class CompositeChannelListener : IChannelListener
    {
        private readonly object _lock = new object();

        private List<IChannelListener> _channelListeners = new List<IChannelListener>();

        public void OnCreate(IModel channel, bool transactional)
        {
            var listeners = _channelListeners;
            foreach (var listener in listeners)
            {
                listener.OnCreate(channel, transactional);
            }
        }

        public void OnShutDown(ShutdownEventArgs @event)
        {
            var listeners = _channelListeners;
            foreach (var listener in listeners)
            {
                listener.OnShutDown(@event);
            }
        }

        public void SetListeners(List<IChannelListener> channelListeners)
        {
            _channelListeners = channelListeners;
        }

        public void AddListener(IChannelListener channelListener)
        {
            lock (_lock)
            {
                var listeners = new List<IChannelListener>(_channelListeners)
                {
                    channelListener
                };
                _channelListeners = listeners;
            }
        }

        public bool RemoveListener(IChannelListener channelListener)
        {
            lock (_lock)
            {
                if (_channelListeners.Contains(channelListener))
                {
                    var listeners = new List<IChannelListener>(_channelListeners);
                    listeners.Remove(channelListener);
                    _channelListeners = listeners;
                    return true;
                }

                return false;
            }
        }

        public void ClearListeners()
        {
            _channelListeners = new List<IChannelListener>();
        }
    }
}
