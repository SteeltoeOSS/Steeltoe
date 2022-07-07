// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public class FixedBackOff : IBackOff
{
    public const int STOP = -1;
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

    private sealed class FixedBackOffExecution : IBackOffExecution
    {
        private readonly FixedBackOff _backoff;
        private int _currentAttempts;

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
                return STOP;
            }
        }

        public override string ToString()
        {
            var attemptValue = _backoff.MaxAttempts == int.MaxValue ? "unlimited" : _backoff.MaxAttempts.ToString();
            return $"FixedBackOff{{interval={_backoff.Interval}, currentAttempts={_currentAttempts}, maxAttempts={attemptValue}}}";
        }
    }
}
