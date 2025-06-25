// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

internal sealed class ComplexDetailsContributor : IHealthContributor
{
    public string Id => "alwaysComplexDetails";

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var result = new HealthCheckResult
        {
            Status = HealthStatus.Up,
            Description = "test-description",
            Details =
            {
                ["ComplexType"] = new TestHealthDetails
                {
                    NestedComplexType = new TestHealthDetails
                    {
                        TestString = "nested-test-string",
                        TestInteger = -1,
                        TestFloatingPoint = 0,
                        TestBoolean = false,
                        TestList = [],
                        TestDictionary = []
                    }
                }
            }
        };

        return Task.FromResult<HealthCheckResult?>(result);
    }

    private sealed class TestHealthDetails
    {
        public string TestString { get; set; } = "test-string";
        public int TestInteger { get; set; } = 123;
        public double TestFloatingPoint { get; set; } = 1.23;
        public bool TestBoolean { get; set; } = true;
        public TestHealthDetails? NestedComplexType { get; set; }

        public List<string> TestList { get; set; } =
        [
            "A",
            "B",
            "C"
        ];

        public Dictionary<string, int> TestDictionary { get; set; } = new()
        {
            ["One"] = 1,
            ["Two"] = 2,
            ["Three"] = 3
        };
    }
}
