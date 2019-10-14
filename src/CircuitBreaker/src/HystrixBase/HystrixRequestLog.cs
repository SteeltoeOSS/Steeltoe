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

using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixRequestLog
    {
        private class HystrixRequestLogVariable : HystrixRequestVariableDefault<HystrixRequestLog>
        {
            public HystrixRequestLogVariable()
                : base(() => new HystrixRequestLog(), (log) =>
                {
                    HystrixRequestEventsStream.GetInstance().Write(log.AllExecutedCommands);
                })
            {
            }
        }

        private static readonly HystrixRequestLogVariable RequestLog = new HystrixRequestLogVariable();

        public static HystrixRequestLog CurrentRequestLog
        {
            get
            {
                if (HystrixRequestContext.IsCurrentThreadInitialized)
                {
                    return RequestLog.Value;
                }
                else
                {
                    return null;
                }
            }
        }

        protected internal const int MAX_STORAGE = 1000;
        private BlockingCollection<IHystrixInvokableInfo> allExecutedCommands = new BlockingCollection<IHystrixInvokableInfo>(MAX_STORAGE);

        internal HystrixRequestLog()
        {
        }

        internal void AddExecutedCommand(IHystrixInvokableInfo command)
        {
            if (!allExecutedCommands.TryAdd(command))
            {
                // see RequestLog: Reduce Chance of Memory Leak https://github.com/Netflix/Hystrix/issues/53
                // logger.warn("RequestLog ignoring command after reaching limit of " + MAX_STORAGE + ". See https://github.com/Netflix/Hystrix/issues/53 for more information.");
            }
        }

        public ICollection<IHystrixInvokableInfo> AllExecutedCommands
        {
            get
            {
                return allExecutedCommands.ToList().AsReadOnly();
            }
        }

        public string GetExecutedCommandsAsString()
        {
            try
            {
                Dictionary<string, int> aggregatedCommandsExecuted = new Dictionary<string, int>();
                Dictionary<string, int> aggregatedCommandExecutionTime = new Dictionary<string, int>();

                StringBuilder builder = new StringBuilder();
                int estimatedLength = 0;
                foreach (IHystrixInvokableInfo command in allExecutedCommands)
                {
                    builder.Length = 0;
                    builder.Append(command.CommandKey.Name);

                    List<HystrixEventType> events = new List<HystrixEventType>(command.ExecutionEvents);
                    if (events.Count > 0)
                    {
                        events.Sort();

                        // replicate functionality of Arrays.toString(events.toArray()) to append directly to existing StringBuilder
                        builder.Append("[");
                        foreach (HystrixEventType ev in events)
                        {
                            switch (ev)
                            {
                                case HystrixEventType.EMIT:
                                    int numEmissions = command.NumberEmissions;
                                    if (numEmissions > 1)
                                    {
                                        builder.Append(ev).Append("x").Append(numEmissions).Append(", ");
                                    }
                                    else
                                    {
                                        builder.Append(ev).Append(", ");
                                    }

                                    break;
                                case HystrixEventType.FALLBACK_EMIT:
                                    int numFallbackEmissions = command.NumberFallbackEmissions;
                                    if (numFallbackEmissions > 1)
                                    {
                                        builder.Append(ev).Append("x").Append(numFallbackEmissions).Append(", ");
                                    }
                                    else
                                    {
                                        builder.Append(ev).Append(", ");
                                    }

                                    break;
                                case HystrixEventType.SUCCESS:
                                case HystrixEventType.FAILURE:
                                case HystrixEventType.TIMEOUT:
                                case HystrixEventType.BAD_REQUEST:
                                case HystrixEventType.SHORT_CIRCUITED:
                                case HystrixEventType.THREAD_POOL_REJECTED:
                                case HystrixEventType.SEMAPHORE_REJECTED:
                                case HystrixEventType.FALLBACK_SUCCESS:
                                case HystrixEventType.FALLBACK_FAILURE:
                                case HystrixEventType.FALLBACK_REJECTION:
                                case HystrixEventType.FALLBACK_MISSING:
                                case HystrixEventType.EXCEPTION_THROWN:
                                case HystrixEventType.RESPONSE_FROM_CACHE:
                                case HystrixEventType.CANCELLED:
                                case HystrixEventType.COLLAPSED:
                                default:
                                    builder.Append(ev).Append(", ");
                                    break;
                            }
                        }

                        builder[builder.Length - 2] = ']';
                        builder.Length -= 1;
                    }
                    else
                    {
                        builder.Append("[Executed]");
                    }

                    string display = builder.ToString();
                    estimatedLength += display.Length + 12; // add 12 chars to display length for appending totalExecutionTime and count below
                    if (aggregatedCommandsExecuted.TryGetValue(display, out int counter))
                    {
                        aggregatedCommandsExecuted[display] = counter + 1;
                    }
                    else
                    {
                        // add it
                        aggregatedCommandsExecuted.Add(display, 1);
                    }

                    int executionTime = command.ExecutionTimeInMilliseconds;
                    if (executionTime < 0)
                    {
                        // do this so we don't create negative values or subtract values
                        executionTime = 0;
                    }

                    counter = 0;
                    if (aggregatedCommandExecutionTime.TryGetValue(display, out counter))
                    {
                        // add to the existing executionTime (sum of executionTimes for duplicate command displayNames)
                        aggregatedCommandExecutionTime[display] = aggregatedCommandExecutionTime[display] + executionTime;
                    }
                    else
                    {
                        // add it
                        aggregatedCommandExecutionTime.Add(display, executionTime);
                    }
                }

                builder.Length = 0;
                builder.EnsureCapacity(estimatedLength);
                foreach (string displayString in aggregatedCommandsExecuted.Keys)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(displayString);
                    int totalExecutionTime = aggregatedCommandExecutionTime[displayString];
                    builder.Append("[").Append(totalExecutionTime).Append("ms]");

                    int count = aggregatedCommandsExecuted[displayString];
                    if (count > 1)
                    {
                        builder.Append("x").Append(count);
                    }
                }

                return builder.ToString();
            }
            catch (Exception)
            {
                // logger.error("Failed to create HystrixRequestLog response header string.", e)
                // don't let this cause the entire app to fail so just return "Unknown"
                return "Unknown";
            }
        }
    }
}
