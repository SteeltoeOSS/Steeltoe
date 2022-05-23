// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCommandOptionsTest
    {
        public static HystrixCommandOptions GetUnitTestOptions()
        {
            return new HystrixCommandOptions()
            {
                ExecutionTimeoutInMilliseconds = 1000, // when an execution will be timed out
                ExecutionTimeoutEnabled = true,
                ExecutionIsolationStrategy = ExecutionIsolationStrategy.THREAD, // we want thread execution by default in tests
                CircuitBreakerForceOpen = false, // we don't want short-circuiting by default
                CircuitBreakerErrorThresholdPercentage = 40, // % of 'marks' that must be failed to trip the circuit
                MetricsRollingStatisticalWindowInMilliseconds = 5000, // milliseconds back that will be tracked
                MetricsRollingStatisticalWindowBuckets = 5, // buckets
                CircuitBreakerRequestVolumeThreshold = 0, // in testing we will not have a threshold unless we're specifically testing that feature
                CircuitBreakerSleepWindowInMilliseconds = 5000000, // milliseconds after tripping circuit before allowing retry (by default set VERY long as we want it to effectively never allow a singleTest for most unit tests)
                CircuitBreakerEnabled = true,
                RequestLogEnabled = true,
                ExecutionIsolationSemaphoreMaxConcurrentRequests = 20,
                FallbackIsolationSemaphoreMaxConcurrentRequests = 10,
                FallbackEnabled = true,
                CircuitBreakerForceClosed = false,
                MetricsRollingPercentileEnabled = true,
                RequestCacheEnabled = true,
                MetricsRollingPercentileWindowInMilliseconds = 60000,
                MetricsRollingPercentileWindowBuckets = 12,
                MetricsRollingPercentileBucketSize = 1000,
                MetricsHealthSnapshotIntervalInMilliseconds = 100
            };
        }

        [Fact]
        public void TestBooleanBuilderOverride1()
        {
            var properties = new HystrixCommandOptions(
                HystrixCommandKeyDefault.AsKey("TEST"),
                new HystrixCommandOptions() { CircuitBreakerForceClosed = true });

            // the builder override should take precedence over the default
            Assert.True(properties.CircuitBreakerForceClosed);
        }

        [Fact]
        public void TestBooleanBuilderOverride2()
        {
            var properties = new HystrixCommandOptions(
                HystrixCommandKeyDefault.AsKey("TEST"),
                new HystrixCommandOptions() { CircuitBreakerForceClosed = false });

            // the builder override should take precedence over the default
            Assert.False(properties.CircuitBreakerForceClosed);
        }

        [Fact]
        public void TestBooleanCodeDefault()
        {
            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), new HystrixCommandOptions());
            Assert.Equal(HystrixCommandOptions.Default_CircuitBreakerForceClosed, properties.CircuitBreakerForceClosed);
        }

        [Fact]
        public void TestBooleanGlobalDynamicOverrideOfCodeDefault()
        {
            var configSettings = @"
            {
                ""hystrix"": {
                    ""command"": {
                        ""default"": {
                            ""circuitBreaker"": {
                                ""forceClosed"": true
                            }
                        }
                    }
                }

            }";
            var memStream = GetMemoryStream(configSettings);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(memStream));
            var config = builder.Build();
            var dynamics = new HystrixDynamicOptionsDefault(config);

            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), null, dynamics);

            // the global dynamic property should take precedence over the default
            Assert.True(properties.CircuitBreakerForceClosed);
        }

        [Fact]
        public void TestBooleanInstanceBuilderOverrideOfGlobalDynamicOverride1()
        {
            var configSettings = @"
            {
                ""hystrix"": {
                    ""command"": {
                        ""default"": {
                            ""circuitBreaker"": {
                                ""forceClosed"": false
                            }
                        }
                    }
                }

            }";
            var memStream = GetMemoryStream(configSettings);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(memStream));
            var config = builder.Build();
            var dynamics = new HystrixDynamicOptionsDefault(config);
            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), new HystrixCommandOptions() { CircuitBreakerForceClosed = true }, dynamics);

            // the builder injected should take precedence over the global dynamic property
            Assert.True(properties.CircuitBreakerForceClosed);
        }

        [Fact]
        public void TestBooleanInstanceBuilderOverrideOfGlobalDynamicOverride2()
        {
            var configSettings = @"
            {
                ""hystrix"": {
                    ""command"": {
                        ""default"": {
                            ""circuitBreaker"": {
                                ""forceClosed"": true
                            }
                        }
                    }
                }

            }";
            var memStream = GetMemoryStream(configSettings);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(memStream));
            var config = builder.Build();
            var dynamics = new HystrixDynamicOptionsDefault(config);
            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), new HystrixCommandOptions() { CircuitBreakerForceClosed = false }, dynamics);

            // the builder injected should take precedence over the global dynamic property
            Assert.False(properties.CircuitBreakerForceClosed);
        }

        [Fact]
        public void TestBooleanInstanceDynamicOverrideOfEverything()
        {
            var configSettings = @"
            {
                ""hystrix"": {
                    ""command"": {
                        ""default"": {
                            ""circuitBreaker"": {
                                ""forceClosed"": false
                            }
                        },
                        ""TEST"": {
                            ""circuitBreaker"": {
                                ""forceClosed"": true
                            }
                        }
                    }
                }

            }";
            var memStream = GetMemoryStream(configSettings);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(memStream));
            var config = builder.Build();
            var dynamics = new HystrixDynamicOptionsDefault(config);

            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), new HystrixCommandOptions() { CircuitBreakerForceClosed = false }, dynamics);

            // the instance specific dynamic property should take precedence over everything
            Assert.True(properties.CircuitBreakerForceClosed);
        }

        [Fact]
        public void TestIntegerBuilderOverride()
        {
            var properties = new HystrixCommandOptions(
                HystrixCommandKeyDefault.AsKey("TEST"),
                new HystrixCommandOptions() { MetricsRollingStatisticalWindowInMilliseconds = 5000 });

            // the builder override should take precedence over the default
            Assert.Equal(5000, properties.MetricsRollingStatisticalWindowInMilliseconds);
        }

        [Fact]
        public void TestIntegerCodeDefault()
        {
            var properties = new HystrixCommandOptions(
                HystrixCommandKeyDefault.AsKey("TEST"),
                new HystrixCommandOptions());
            Assert.Equal(HystrixCommandOptions.Default_MetricsRollingStatisticalWindow, properties.MetricsRollingStatisticalWindowInMilliseconds);
        }

        [Fact]
        public void TestIntegerGlobalDynamicOverrideOfCodeDefault()
        {
            var configSettings = @"
            {
                ""hystrix"": {
                    ""command"": {
                        ""default"": {
                            ""metrics"": {
                                ""rollingStats"": {
                                    ""timeInMilliseconds"": 1234
                                }
                            }
                        }
                    }
                }

            }";
            var memStream = GetMemoryStream(configSettings);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(memStream));
            var config = builder.Build();
            var dynamics = new HystrixDynamicOptionsDefault(config);

            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), null, dynamics);
            //// the global dynamic property should take precedence over the default
            Assert.Equal(1234, properties.MetricsRollingStatisticalWindowInMilliseconds);
        }

        [Fact]
        public void TestIntegerInstanceBuilderOverrideOfGlobalDynamicOverride()
        {
            var configSettings = @"
            {
                ""hystrix"": {
                    ""command"": {
                        ""default"": {
                            ""metrics"": {
                                ""rollingStats"": {
                                    ""timeInMilliseconds"": 3456
                                }
                            }
                        }
                    }
                }

            }";
            var memStream = GetMemoryStream(configSettings);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(memStream));
            var config = builder.Build();
            var dynamics = new HystrixDynamicOptionsDefault(config);

            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), new HystrixCommandOptions() { MetricsRollingStatisticalWindowInMilliseconds = 5000 }, dynamics);

            // the builder injected should take precedence over the global dynamic property
            Assert.Equal(5000, properties.MetricsRollingStatisticalWindowInMilliseconds);
        }

        [Fact]
        public void TestIntegerInstanceDynamicOverrideOfEverything()
        {
            var configSettings = @"
            {
                ""hystrix"": {
                    ""command"": {
                        ""default"": {
                            ""metrics"": {
                                ""rollingStats"": {
                                    ""timeInMilliseconds"": 1234
                                }
                            }
                        },
                        ""TEST"": {
                            ""metrics"": {
                                ""rollingStats"": {
                                    ""timeInMilliseconds"": 3456
                                }
                            }
                        }
                    }
                }

            }";
            var memStream = GetMemoryStream(configSettings);
            var builder = new ConfigurationBuilder();
            builder.Add(new JsonStreamConfigurationSource(memStream));
            var config = builder.Build();
            var dynamics = new HystrixDynamicOptionsDefault(config);

            var properties = new HystrixCommandOptions(HystrixCommandKeyDefault.AsKey("TEST"), new HystrixCommandOptions() { MetricsRollingStatisticalWindowInMilliseconds = 5000 }, dynamics);

            // the instance specific dynamic property should take precedence over everything
            Assert.Equal(3456, properties.MetricsRollingStatisticalWindowInMilliseconds);
        }

        internal static MemoryStream GetMemoryStream(string json)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(json);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }

        private class JsonStreamConfigurationProvider : JsonConfigurationProvider
        {
            private readonly MemoryStream _stream;

            internal JsonStreamConfigurationProvider(JsonConfigurationSource source, MemoryStream stream)
                : base(source)
            {
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public override void Load()
            {
                Load(_stream);
            }
        }

        private class JsonStreamConfigurationSource : JsonConfigurationSource
        {
            private readonly MemoryStream _stream;

            internal JsonStreamConfigurationSource(MemoryStream stream)
            {
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public override IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                return new JsonStreamConfigurationProvider(this, _stream);
            }
        }
    }
}
