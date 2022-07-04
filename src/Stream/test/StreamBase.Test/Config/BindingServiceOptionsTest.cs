// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Stream.Config;

public class BindingServiceOptionsTest
{
    [Fact]
    public void Initialize_ConfiguresOptionsCorrectly()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:cloud:stream:instanceCount", "100" },
            { "spring:cloud:stream:instanceIndex", "1" },
            { "spring:cloud:stream:dynamicDestinations:0", "dynamicDestinations" },
            { "spring:cloud:stream:defaultBinder", "defaultBinder" },
            { "spring:cloud:stream:overrideCloudConnectors", "true" },
            { "spring:cloud:stream:bindingRetryInterval", "500" },
            { "spring:cloud:stream:default:destination", "destination" },
            { "spring:cloud:stream:default:group", "group" },
            { "spring:cloud:stream:default:contentType", "contentType" },
            { "spring:cloud:stream:default:binder", "binder" },
            { "spring:cloud:stream:bindings:input:destination", "inputdestination" },
            { "spring:cloud:stream:bindings:input:group", "inputgroup" },
            { "spring:cloud:stream:bindings:input:contentType", "inputcontentType" },
            { "spring:cloud:stream:bindings:input:binder", "inputbinder" },
            { "spring:cloud:stream:bindings:input:consumer:autoStartup", "false" },
            { "spring:cloud:stream:bindings:input:consumer:concurrency", "10" },
            { "spring:cloud:stream:bindings:input:consumer:partitioned", "true" },
            { "spring:cloud:stream:bindings:input:consumer:headerMode", "headers" },
            { "spring:cloud:stream:bindings:input:consumer:maxAttempts", "10" },
            { "spring:cloud:stream:bindings:input:consumer:backOffInitialInterval", "10" },
            { "spring:cloud:stream:bindings:input:consumer:backOffMaxInterval", "10" },
            { "spring:cloud:stream:bindings:input:consumer:backOffMultiplier", "5.0" },
            { "spring:cloud:stream:bindings:input:consumer:defaultRetryable", "false" },
            { "spring:cloud:stream:bindings:input:consumer:instanceIndex", "10" },
            { "spring:cloud:stream:bindings:input:consumer:instanceCount", "10" },
            { "spring:cloud:stream:bindings:input:consumer:retryableExceptions:0", "notused" },
            { "spring:cloud:stream:bindings:input:consumer:useNativeDecoding", "true" },
            { "spring:cloud:stream:bindings:output:destination", "outputdestination" },
            { "spring:cloud:stream:bindings:output:group", "outputgroup" },
            { "spring:cloud:stream:bindings:output:contentType", "outputcontentType" },
            { "spring:cloud:stream:bindings:output:binder", "outputbinder" },
            { "spring:cloud:stream:bindings:output:producer:autoStartup", "false" },
            { "spring:cloud:stream:bindings:output:producer:partitionKeyExpression", "partitionKeyExpression" },
            { "spring:cloud:stream:bindings:output:producer:partitionSelectorExpression", "partitionSelectorExpression" },
            { "spring:cloud:stream:bindings:output:producer:partitionKeyExtractorName", "partitionKeyExtractorName" },
            { "spring:cloud:stream:bindings:output:producer:partitionSelectorName", "partitionSelectorName" },
            { "spring:cloud:stream:bindings:output:producer:partitionCount", "10" },
            { "spring:cloud:stream:bindings:output:producer:requiredGroups:0", "requiredGroups" },
            { "spring:cloud:stream:bindings:output:producer:headerMode", "headers" },
            { "spring:cloud:stream:bindings:output:producer:useNativeEncoding", "true" },
            { "spring:cloud:stream:bindings:output:producer:errorChannelEnabled", "true" },
            { "spring:cloud:stream:binders:foobar:inheritEnvironment", "false" },
            { "spring:cloud:stream:binders:foobar:defaultCandidate", "false" },
            { "spring:cloud:stream:binders:foobar:environment:key1", "value1" },
            { "spring:cloud:stream:binders:foobar:environment:key2", "value2" },
        });

        var config = builder.Build().GetSection("spring:cloud:stream");
        var options = new BindingServiceOptions(config);
        options.PostProcess();

        Assert.Equal(100, options.InstanceCount);
        Assert.Equal(1, options.InstanceIndex);
        Assert.Equal("dynamicDestinations", options.DynamicDestinations[0]);
        Assert.Equal("defaultBinder", options.DefaultBinder);
        Assert.Equal(true, options.OverrideCloudConnectors);
        Assert.Equal(500, options.BindingRetryInterval);

        Assert.NotNull(options.Default);
        Assert.Equal("destination", options.Default.Destination);
        Assert.Equal("group", options.Default.Group);
        Assert.Equal("contentType", options.Default.ContentType);
        Assert.Equal("binder", options.Default.Binder);

        var input = options.GetBindingOptions("input");

        Assert.NotNull(input);
        Assert.Equal("inputdestination", input.Destination);
        Assert.Equal("inputgroup", input.Group);
        Assert.Equal("inputcontentType", input.ContentType);
        Assert.Equal("inputbinder", input.Binder);
        Assert.Equal(false, input.Consumer.AutoStartup);
        Assert.Equal(10, input.Consumer.Concurrency);
        Assert.Equal(true, input.Consumer.Partitioned);
        Assert.Equal(HeaderMode.Headers, input.Consumer.HeaderMode);
        Assert.Equal(10, input.Consumer.MaxAttempts);
        Assert.Equal(10, input.Consumer.BackOffInitialInterval);
        Assert.Equal(10, input.Consumer.BackOffMaxInterval);
        Assert.Equal(5.0, input.Consumer.BackOffMultiplier);
        Assert.Equal(false, input.Consumer.DefaultRetryable);
        Assert.Equal(10, input.Consumer.InstanceIndex);
        Assert.Equal(10, input.Consumer.InstanceCount);
        Assert.Equal(new List<string> { "notused" }, input.Consumer.RetryableExceptions);
        Assert.Equal(true, input.Consumer.UseNativeDecoding);

        var output = options.GetBindingOptions("output");

        Assert.Equal("outputdestination", output.Destination);
        Assert.Equal("outputgroup", output.Group);
        Assert.Equal("outputcontentType", output.ContentType);
        Assert.Equal("outputbinder", output.Binder);
        Assert.Equal(false, output.Producer.AutoStartup);
        Assert.Equal("partitionKeyExpression", output.Producer.PartitionKeyExpression);
        Assert.Equal("partitionSelectorExpression", output.Producer.PartitionSelectorExpression);
        Assert.Equal("partitionKeyExtractorName", output.Producer.PartitionKeyExtractorName);
        Assert.Equal("partitionSelectorName", output.Producer.PartitionSelectorName);
        Assert.Equal(10, output.Producer.PartitionCount);
        Assert.Equal("requiredGroups", output.Producer.RequiredGroups[0]);
        Assert.Equal(HeaderMode.Headers, output.Producer.HeaderMode);
        Assert.Equal(true, output.Producer.UseNativeEncoding);
        Assert.Equal(true, output.Producer.ErrorChannelEnabled);

        var foobar = options.Binders["foobar"];
        Assert.NotNull(foobar);

        Assert.Equal(false, foobar.InheritEnvironment);
        Assert.Equal(false, foobar.DefaultCandidate);
        Assert.Equal("value1", foobar.Environment["key1"]);
        Assert.Equal("value2", foobar.Environment["key2"]);
    }

    [Fact]
    public void Defaults_ConfiguresOptionsCorrectly()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string>());
        var configuration = builder.Build().GetSection(BindingServiceOptions.Prefix);
        var options = new BindingServiceOptions(configuration);
        options.PostProcess();

        Assert.Equal(1, options.InstanceCount);
        Assert.Equal(0, options.InstanceIndex);
        Assert.False(options.OverrideCloudConnectors);
        Assert.Empty(options.DynamicDestinations);
        Assert.Null(options.DefaultBinder);
        Assert.Equal(30, options.BindingRetryInterval);
        Assert.Empty(options.Binders);
        Assert.Empty(options.Bindings);
        Assert.Null(options.GetBinder("foobar"));
        Assert.NotNull(options.GetBindingOptions("foobar")); // Creates binding with that name using defaults
        Assert.Equal("foobar", options.GetBindingDestination("foobar"));
        Assert.NotNull(options.GetConsumerOptions("foobar"));
        Assert.Null(options.GetGroup("foobar"));
        Assert.NotNull(options.GetProducerOptions("foobar"));
    }

    [Fact]
    public void NonDefaults_ConfiguresOptionsCorrectly()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:cloud:stream:instanceCount", "100" },
            { "spring:cloud:stream:instanceIndex", "1" },
            { "spring:cloud:stream:dynamicDestinations:0", "dynamicDestinations" },
            { "spring:cloud:stream:defaultBinder", "defaultBinder" },
            { "spring:cloud:stream:overrideCloudConnectors", "true" },
            { "spring:cloud:stream:bindingRetryInterval", "500" },
        });

        var configuration = builder.Build().GetSection(BindingServiceOptions.Prefix);
        var options = new BindingServiceOptions(configuration);
        options.PostProcess();

        Assert.Equal(100, options.InstanceCount);
        Assert.Equal(1, options.InstanceIndex);
        Assert.True(options.OverrideCloudConnectors);
        Assert.Single(options.DynamicDestinations);
        Assert.Equal("dynamicDestinations", options.DynamicDestinations[0]);
        Assert.Equal("defaultBinder", options.DefaultBinder);
        Assert.Equal(500, options.BindingRetryInterval);
        Assert.Empty(options.Binders);
        Assert.Empty(options.Bindings);
        Assert.Null(options.GetBinder("foobar"));
        Assert.NotNull(options.GetBindingOptions("foobar")); // Creates binding with that name using defaults
        Assert.Equal("foobar", options.GetBindingDestination("foobar"));
        Assert.NotNull(options.GetConsumerOptions("foobar"));
        Assert.Null(options.GetGroup("foobar"));
        Assert.NotNull(options.GetProducerOptions("foobar"));
    }

    [Fact]
    public void Mixture_Default_NonDefault_ConfiguresOptionsCorrectly()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:cloud:stream:instanceIndex", "2" },
            { "spring:cloud:stream:dynamicDestinations:0", "dynamicDestinations" },
            { "spring:cloud:stream:overrideCloudConnectors", "true" },
            { "spring:cloud:stream:bindingRetryInterval", "100" },
        });

        var configuration = builder.Build().GetSection(BindingServiceOptions.Prefix);
        var options = new BindingServiceOptions(configuration);
        options.PostProcess();

        Assert.Equal(1, options.InstanceCount);
        Assert.Equal(2, options.InstanceIndex);
        Assert.True(options.OverrideCloudConnectors);
        Assert.Single(options.DynamicDestinations);
        Assert.Equal("dynamicDestinations", options.DynamicDestinations[0]);
        Assert.Null(options.DefaultBinder);
        Assert.Equal(100, options.BindingRetryInterval);
        Assert.Empty(options.Binders);
        Assert.Empty(options.Bindings);
        Assert.Null(options.GetBinder("foobar"));
        Assert.NotNull(options.GetBindingOptions("foobar")); // Creates binding with that name using defaults
        Assert.Equal("foobar", options.GetBindingDestination("foobar"));
        Assert.NotNull(options.GetConsumerOptions("foobar"));
        Assert.Null(options.GetGroup("foobar"));
        Assert.NotNull(options.GetProducerOptions("foobar"));
    }
}
