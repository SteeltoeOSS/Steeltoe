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

namespace Steeltoe.Messaging.Rabbit.Util
{
    // TODO: Move to common
    public class FixedBackOff : IBackOff
    {
        public const int DEFAULT_INTERVAL = 5000;
        public const int UNLIMITED_ATTEMPTS = int.MaxValue;

        public FixedBackOff()
        {
        }

        public FixedBackOff(int interval, int maxAttempts)
        {
            Interval = interval;
            MaxAttempts = maxAttempts;
        }

        public int Interval { get; set; } = DEFAULT_INTERVAL;

        public int MaxAttempts { get; set; } = UNLIMITED_ATTEMPTS;

        public IBackOffExecution Start()
        {
            return new FixedBackOffExecution(this);
        }

        private class FixedBackOffExecution : IBackOffExecution
        {
            private readonly FixedBackOff _backoff;
            private int _currentAttempts = 0;

            public FixedBackOffExecution(FixedBackOff backoff)
            {
                _backoff = backoff;
            }

            public int NextBackOff()
            {
                _currentAttempts++;
                if (_currentAttempts <= _backoff.MaxAttempts)
                {
                    return _backoff.Interval;
                }
                else
                {
                    return IBackOffExecution.STOP;
                }
            }

            public override string ToString()
            {
                var attemptValue = (_backoff.MaxAttempts == int.MaxValue) ? "unlimited" : _backoff.MaxAttempts.ToString();
                return "FixedBackOff{interval=" + _backoff.Interval +
                        ", currentAttempts=" + _currentAttempts +
                        ", maxAttempts=" + attemptValue +
                        '}';
            }
        }
    }
}
