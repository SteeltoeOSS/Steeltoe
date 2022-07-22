// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Binding;

public static class StreamListenerErrorMessages
{
    public const string INVALID_INBOUND_NAME = "The Input attribute must have the name of an input as value";

    public const string INVALID_OUTBOUND_NAME = "The Output attribute must have the name of an output as value";

    public const string ATLEAST_ONE_OUTPUT = "At least one output must be specified";

    public const string SEND_TO_MULTIPLE_DESTINATIONS = "Multiple destinations cannot be specified";

    public const string SEND_TO_EMPTY_DESTINATION = "An empty destination cannot be specified";

    public const string INVALID_INPUT_OUTPUT_METHOD_PARAMETERS = "Input or Output attribute "
                                                                 + "are not permitted on "
                                                                 + "method parameters while using the StreamListener value and a method-level output specification";

    public const string NO_INPUT_DESTINATION = "No input destination is configured. "
                                               + "Use either the StreamListener value or Input";

    public const string AMBIGUOUS_MESSAGE_HANDLER_METHOD_ARGUMENTS = "Ambiguous method arguments "
                                                                     + "for the StreamListener method";

    public const string INVALID_INPUT_VALUES = "Cannot set both StreamListener "
                                               + "value and Input attribute as method parameter";

    public const string INVALID_INPUT_VALUE_WITH_OUTPUT_METHOD_PARAM = "Setting the StreamListener "
                                                                       + "value when using Output attribute as method parameter is not permitted. "
                                                                       + "Use Input method parameter attribute to specify inbound value instead";

    public const string INVALID_OUTPUT_VALUES = "Cannot set both output (Output/SendTo) method attribute value"
                                                + " and Output attribute as a method parameter";

    public const string CONDITION_ON_DECLARATIVE_METHOD = "Cannot set a condition when "
                                                          + "using StreamListener in declarative mode";

    public const string CONDITION_ON_METHOD_RETURNING_VALUE = "Cannot set a condition "
                                                              + "for methods that return a value";

    public const string MULTIPLE_VALUE_RETURNING_METHODS = "If multiple StreamListener "
                                                           + "methods are listening to the same binding target, none of them may return a value";

    public const string INPUT_AT_STREAM_LISTENER = PREFIX
                                                   + "may never be annotated with Input. "
                                                   + "If it should listen to a specific input, use the value of StreamListener instead";

    public const string RETURN_TYPE_NO_OUTBOUND_SPECIFIED = PREFIX
                                                            + "having a return type should also have an outbound target specified";

    public const string RETURN_TYPE_MULTIPLE_OUTBOUND_SPECIFIED = PREFIX
                                                                  + "having a return type should have only one outbound target specified";

    public const string INVALID_DECLARATIVE_METHOD_PARAMETERS = PREFIX
                                                                + "may use Input or Output attributes only in declarative mode "
                                                                + "and for parameters that are binding targets or convertible from binding targets.";

    private const string PREFIX = "A method attributed with StreamListener ";
}