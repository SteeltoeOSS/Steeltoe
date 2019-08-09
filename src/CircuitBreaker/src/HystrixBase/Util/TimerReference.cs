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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class TimerReference : IDisposable
    {
        internal ITimerListener _listener;
        internal CancellationTokenSource _tokenSource;
        internal TimeSpan _period;
        internal Task _timerTask;

        public TimerReference(ITimerListener listener, TimeSpan period)
        {
            _listener = listener;
            _tokenSource = new CancellationTokenSource();
            _period = period;
            _timerTask = new Task(() => { Run(_tokenSource); }, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
            _timerTask.Start();
        }

        public void Run(CancellationTokenSource tokenSource)
        {
            while (!tokenSource.IsCancellationRequested)
            {
                Time.WaitUntil(() => { return tokenSource.IsCancellationRequested; }, (int)_period.TotalMilliseconds);

                if (!tokenSource.IsCancellationRequested)
                {
                    _listener.Tick();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
            }

            _listener = null;
            _timerTask = null;
        }
    }
}
