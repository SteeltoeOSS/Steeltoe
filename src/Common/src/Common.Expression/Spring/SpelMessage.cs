// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class SpelMessage
{
    public static readonly SpelMessage TypeConversionError = new (
        Kind.Error, 1001, "Type conversion problem, cannot convert from {0} to {1}");

    public static readonly SpelMessage ConstructorNotFound = new (
        Kind.Error, 1002, "Constructor call: No suitable constructor found on type {0} for arguments {1}");

    public static readonly SpelMessage ConstructorInvocationProblem = new (
        Kind.Error, 1003, "A problem occurred whilst attempting to construct an object of type ''{0}'' using arguments ''{1}''");

    public static readonly SpelMessage MethodNotFound = new (
        Kind.Error, 1004, "Method call: Method {0} cannot be found on type {1}");

    public static readonly SpelMessage TypeNotFound = new (
        Kind.Error, 1005, "Type cannot be found ''{0}''");

    public static readonly SpelMessage FunctionNotDefined = new (
        Kind.Error, 1006, "Function ''{0}'' could not be found");

    public static readonly SpelMessage PropertyOrFieldNotReadableOnNull = new (
        Kind.Error, 1007, "Property or field ''{0}'' cannot be found on null");

    public static readonly SpelMessage PropertyOrFieldNotReadable = new (
        Kind.Error, 1008, "Property or field ''{0}'' cannot be found on object of type ''{1}'' - maybe not public or not valid?");

    public static readonly SpelMessage PropertyOrFieldNotWritableOnNull = new (
        Kind.Error, 1009, "Property or field ''{0}'' cannot be set on null");

    public static readonly SpelMessage PropertyOrFieldNotWritable = new (
        Kind.Error, 1010, "Property or field ''{0}'' cannot be set on object of type ''{1}'' - maybe not public or not writable?");

    public static readonly SpelMessage MethodCallOnNullObjectNotAllowed = new (
        Kind.Error, 1011, "Method call: Attempted to call method {0} on null context object");

    public static readonly SpelMessage CannotIndexIntoNullValue = new (
        Kind.Error, 1012, "Cannot index into a null value");

    public static readonly SpelMessage NotComparable = new (
        Kind.Error, 1013, "Cannot compare instances of {0} and {1}");

    public static readonly SpelMessage IncorrectNumberOfArgumentsToFunction = new (
        Kind.Error, 1014, "Incorrect number of arguments for function, {0} supplied but function takes {1}");

    public static readonly SpelMessage InvalidTypeForSelection = new (
        Kind.Error, 1015, "Cannot perform selection on input data of type ''{0}''");

    public static readonly SpelMessage ResultOfSelectionCriteriaIsNotBoolean = new (
        Kind.Error, 1016, "Result of selection criteria is not boolean");

    public static readonly SpelMessage BetweenRightOperandMustBeTwoElementList = new (
        Kind.Error, 1017, "Right operand for the 'between' operator has to be a two-element list");

    public static readonly SpelMessage InvalidPattern = new (
        Kind.Error, 1018, "Pattern is not valid ''{0}''");

    public static readonly SpelMessage ProjectionNotSupportedOnType = new (
        Kind.Error, 1019, "Projection is not supported on the type ''{0}''");

    public static readonly SpelMessage ArglistShouldNotBeEvaluated = new (
        Kind.Error, 1020, "The argument list of a lambda expression should never have getValue() called upon it");

    public static readonly SpelMessage ExceptionDuringPropertyRead = new (
        Kind.Error, 1021, "A problem occurred whilst attempting to access the property ''{0}'': ''{1}''");

    public static readonly SpelMessage FunctionReferenceCannotBeInvoked = new (
        Kind.Error, 1022, "The function ''{0}'' mapped to an object of type ''{1}'' which cannot be invoked");

    public static readonly SpelMessage ExceptionDuringFunctionCall = new (
        Kind.Error, 1023, "A problem occurred whilst attempting to invoke the function ''{0}'': ''{1}''");

    public static readonly SpelMessage ArrayIndexOutOfBounds = new (
        Kind.Error, 1024, "The array has ''{0}'' elements, index ''{1}'' is invalid");

    public static readonly SpelMessage CollectionIndexOutOfBounds = new (
        Kind.Error, 1025, "The collection has ''{0}'' elements, index ''{1}'' is invalid");

    public static readonly SpelMessage StringIndexOutOfBounds = new (
        Kind.Error, 1026, "The string has ''{0}'' characters, index ''{1}'' is invalid");

    public static readonly SpelMessage IndexingNotSupportedForType = new (
        Kind.Error, 1027, "Indexing into type ''{0}'' is not supported");

    public static readonly SpelMessage InstanceofOperatorNeedsClassOperand = new (
        Kind.Error, 1028, "The operator 'instanceof' needs the right operand to be a class, not a ''{0}''");

    public static readonly SpelMessage ExceptionDuringMethodInvocation = new (
        Kind.Error, 1029, "A problem occurred when trying to execute method ''{0}'' on object of type ''{1}'': ''{2}''");

    public static readonly SpelMessage OperatorNotSupportedBetweenTypes = new (
        Kind.Error, 1030, "The operator ''{0}'' is not supported between objects of type ''{1}'' and ''{2}''");

    public static readonly SpelMessage ProblemLocatingMethod = new (
        Kind.Error, 1031, "Problem locating method {0} on type {1}");

    public static readonly SpelMessage SetvalueNotSupported = new (
        Kind.Error, 1032, "setValue(ExpressionState, Object) not supported for ''{0}''");

    public static readonly SpelMessage MultiplePossibleMethods = new (
        Kind.Error, 1033, "Method call of ''{0}'' is ambiguous, supported type conversions allow multiple variants to match");

    public static readonly SpelMessage ExceptionDuringPropertyWrite = new (
        Kind.Error, 1034, "A problem occurred whilst attempting to set the property ''{0}'': {1}");

    public static readonly SpelMessage NotAnInteger = new (
        Kind.Error, 1035, "The value ''{0}'' cannot be parsed as an int");

    public static readonly SpelMessage NotALong = new (
        Kind.Error, 1036, "The value ''{0}'' cannot be parsed as a long");

    public static readonly SpelMessage InvalidFirstOperandForMatchesOperator = new (
        Kind.Error, 1037, "First operand to matches operator must be a string. ''{0}'' is not");

    public static readonly SpelMessage InvalidSecondOperandForMatchesOperator = new (
        Kind.Error, 1038, "Second operand to matches operator must be a string. ''{0}'' is not");

    public static readonly SpelMessage FunctionMustBeStatic = new (
        Kind.Error, 1039, "Only static methods can be called via function references. The method ''{0}'' referred to by name ''{1}'' is not static.");

    public static readonly SpelMessage NotAReal = new (
        Kind.Error, 1040, "The value ''{0}'' cannot be parsed as a double");

    public static readonly SpelMessage MoreInput = new (
        Kind.Error, 1041, "After parsing a valid expression, there is still more data in the expression: ''{0}''");

    public static readonly SpelMessage RightOperandProblem = new (
        Kind.Error, 1042, "Problem parsing right operand");

    public static readonly SpelMessage NotExpectedToken = new (
        Kind.Error, 1043, "Unexpected token. Expected ''{0}'' but was ''{1}''");

    public static readonly SpelMessage Ood = new (
        Kind.Error, 1044, "Unexpectedly ran out of input");

    public static readonly SpelMessage NonTerminatingDoubleQuotedString = new (
        Kind.Error, 1045, "Cannot find terminating \" for string");

    public static readonly SpelMessage NonTerminatingQuotedString = new (
        Kind.Error, 1046, "Cannot find terminating '' for string");

    public static readonly SpelMessage MissingLeadingZeroForNumber = new (
        Kind.Error, 1047, "A real number must be prefixed by zero, it cannot start with just ''.''");

    public static readonly SpelMessage RealCannotBeLong = new (
        Kind.Error, 1048, "Real number cannot be suffixed with a long (L or l) suffix");

    public static readonly SpelMessage UnexpectedDataAfterDot = new (
        Kind.Error, 1049, "Unexpected data after ''.'': ''{0}''");

    public static readonly SpelMessage MissingConstructorArgs = new (
        Kind.Error, 1050, "The arguments '(...)' for the constructor call are missing");

    public static readonly SpelMessage RunOutOfArguments = new (
        Kind.Error, 1051, "Unexpectedly ran out of arguments");

    public static readonly SpelMessage UnableToGrowCollection = new (
        Kind.Error, 1052, "Unable to grow collection");

    public static readonly SpelMessage UnableToGrowCollectionUnknownElementType = new (
        Kind.Error, 1053, "Unable to grow collection: unable to determine list element type");

    public static readonly SpelMessage UnableToCreateListForIndexing = new (
        Kind.Error, 1054, "Unable to dynamically create a List to replace a null value");

    public static readonly SpelMessage UnableToCreateMapForIndexing = new (
        Kind.Error, 1055, "Unable to dynamically create a Map to replace a null value");

    public static readonly SpelMessage UnableToDynamicallyCreateObject = new (
        Kind.Error, 1056, "Unable to dynamically create instance of ''{0}'' to replace a null value");

    public static readonly SpelMessage NoServiceResolverRegistered = new (
        Kind.Error, 1057, "No service resolver registered in the context to resolve access to service ''{0}''");

    public static readonly SpelMessage ExceptionDuringServiceResolution = new (
        Kind.Error, 1058, "A problem occurred when trying to resolve service ''{0}'':''{1}''");

    public static readonly SpelMessage InvalidServiceReference = new (
        Kind.Error, 1059, "@ or & can only be followed by an identifier or a quoted name");

    public static readonly SpelMessage TypeNameExpectedForArrayConstruction = new (
        Kind.Error, 1060, "Expected the type of the new array to be specified as a String but found ''{0}''");

    public static readonly SpelMessage IncorrectElementTypeForArray = new (
        Kind.Error, 1061, "The array of type ''{0}'' cannot have an element of type ''{1}'' inserted");

    public static readonly SpelMessage MultidimArrayInitializerNotSupported = new (
        Kind.Error, 1062, "Using an initializer to build a multi-dimensional array is not currently supported");

    public static readonly SpelMessage MissingArrayDimension = new (
        Kind.Error, 1063, "A required array dimension has not been specified");

    public static readonly SpelMessage InitializerLengthIncorrect = new (
        Kind.Error, 1064, "Array initializer size does not match array dimensions");

    public static readonly SpelMessage UnexpectedEscapeChar = new (
        Kind.Error, 1065, "Unexpected escape character");

    public static readonly SpelMessage OperandNotIncrementable = new (
        Kind.Error, 1066, "The expression component ''{0}'' does not support increment");

    public static readonly SpelMessage OperandNotDecrementable = new (
        Kind.Error, 1067, "The expression component ''{0}'' does not support decrement");

    public static readonly SpelMessage NotAssignable = new (
        Kind.Error, 1068, "The expression component ''{0}'' is not assignable");

    public static readonly SpelMessage MissingCharacter = new (
        Kind.Error, 1069, "Missing expected character ''{0}''");

    public static readonly SpelMessage LeftOperandProblem = new (
        Kind.Error, 1070, "Problem parsing left operand");

    public static readonly SpelMessage MissingSelectionExpression = new (
        Kind.Error, 1071, "A required selection expression has not been specified");

    public static readonly SpelMessage ExceptionRunningCompiledExpression = new (
        Kind.Error, 1072, "An exception occurred whilst evaluating a compiled expression");

    public static readonly SpelMessage FlawedPattern = new (
        Kind.Error, 1073, "Failed to efficiently evaluate pattern ''{0}'': consider redesigning it");

    public enum Kind
    {
        Info,
        Warning,
        Error
    }

    private readonly Kind _kind;
    private readonly int _code;
    private readonly string _message;

    private SpelMessage(Kind kind, int code, string message)
    {
        _kind = kind;
        _code = code;
        _message = message;
    }

    public string FormatMessage(params object[] inserts)
    {
        var formattedMessage = new StringBuilder();
        formattedMessage.Append("EL").Append(_code);
        switch (_kind)
        {
            case Kind.Error:
                formattedMessage.Append('E');
                break;
        }

        formattedMessage.Append(": ");
        formattedMessage.Append(string.Format(_message, inserts));
        return formattedMessage.ToString();
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not SpelMessage asMessage)
        {
            return false;
        }

        return _kind == asMessage._kind &&
               _code == asMessage._code &&
               _message == asMessage._message;
    }

    public override int GetHashCode()
    {
        return _message.GetHashCode();
    }
}
