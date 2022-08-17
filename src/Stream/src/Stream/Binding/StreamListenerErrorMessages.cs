// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binding;

public static class StreamListenerErrorMessages
{
    private const string Prefix = "A method attributed with StreamListener ";

    public const string InvalidInboundName = "The Input attribute must have the name of an input as value";

    public const string InvalidOutboundName = "The Output attribute must have the name of an output as value";

    public const string AtLeastOneOutput = "At least one output must be specified";

    public const string SendToMultipleDestinations = "Multiple destinations cannot be specified";

    public const string SendToEmptyDestination = "An empty destination cannot be specified";

    public const string InvalidInputOutputMethodParameters = "Input or Output attribute are not permitted on " +
        "method parameters while using the StreamListener value and a method-level output specification";

    public const string NoInputDestination = "No input destination is configured. Use either the StreamListener value or Input";

    public const string AmbiguousMessageHandlerMethodArguments = "Ambiguous method arguments for the StreamListener method";

    public const string InvalidInputValues = "Cannot set both StreamListener value and Input attribute as method parameter";

    public const string InvalidInputValueWithOutputMethodParam = "Setting the StreamListener value when using Output attribute " +
        "as method parameter is not permitted. Use Input method parameter attribute to specify inbound value instead";

    public const string InvalidOutputValues = "Cannot set both output (Output/SendTo) method attribute value and Output attribute as a method parameter";

    public const string ConditionOnDeclarativeMethod = "Cannot set a condition when using StreamListener in declarative mode";

    public const string ConditionOnMethodReturningValue = "Cannot set a condition for methods that return a value";

    public const string MultipleValueReturningMethods =
        "If multiple StreamListener methods are listening to the same binding target, none of them may return a value";

    public const string InputAtStreamListener =
        $"{Prefix}may never be annotated with Input. If it should listen to a specific input, use the value of StreamListener instead";

    public const string ReturnTypeNoOutboundSpecified = $"{Prefix}having a return type should also have an outbound target specified";

    public const string ReturnTypeMultipleOutboundSpecified = $"{Prefix}having a return type should have only one outbound target specified";

    public const string InvalidDeclarativeMethodParameters =
        $"{Prefix}may use Input or Output attributes only in declarative mode and for parameters that are binding targets or convertible from binding targets.";
}
