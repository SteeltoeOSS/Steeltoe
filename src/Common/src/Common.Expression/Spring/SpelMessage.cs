// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Spring
{
    public class SpelMessage
    {
        public static readonly SpelMessage TYPE_CONVERSION_ERROR = new SpelMessage(
            Kind.ERROR, 1001, "Type conversion problem, cannot convert from {0} to {1}");

        public static readonly SpelMessage CONSTRUCTOR_NOT_FOUND = new SpelMessage(
            Kind.ERROR, 1002, "Constructor call: No suitable constructor found on type {0} for arguments {1}");

        public static readonly SpelMessage CONSTRUCTOR_INVOCATION_PROBLEM = new SpelMessage(
            Kind.ERROR, 1003, "A problem occurred whilst attempting to construct an object of type ''{0}'' using arguments ''{1}''");

        public static readonly SpelMessage METHOD_NOT_FOUND = new SpelMessage(
            Kind.ERROR, 1004, "Method call: Method {0} cannot be found on type {1}");

        public static readonly SpelMessage TYPE_NOT_FOUND = new SpelMessage(
            Kind.ERROR, 1005, "Type cannot be found ''{0}''");

        public static readonly SpelMessage FUNCTION_NOT_DEFINED = new SpelMessage(
            Kind.ERROR, 1006, "Function ''{0}'' could not be found");

        public static readonly SpelMessage PROPERTY_OR_FIELD_NOT_READABLE_ON_NULL = new SpelMessage(
            Kind.ERROR, 1007, "Property or field ''{0}'' cannot be found on null");

        public static readonly SpelMessage PROPERTY_OR_FIELD_NOT_READABLE = new SpelMessage(
            Kind.ERROR, 1008, "Property or field ''{0}'' cannot be found on object of type ''{1}'' - maybe not public or not valid?");

        public static readonly SpelMessage PROPERTY_OR_FIELD_NOT_WRITABLE_ON_NULL = new SpelMessage(
            Kind.ERROR, 1009, "Property or field ''{0}'' cannot be set on null");

        public static readonly SpelMessage PROPERTY_OR_FIELD_NOT_WRITABLE = new SpelMessage(
            Kind.ERROR, 1010, "Property or field ''{0}'' cannot be set on object of type ''{1}'' - maybe not public or not writable?");

        public static readonly SpelMessage METHOD_CALL_ON_NULL_OBJECT_NOT_ALLOWED = new SpelMessage(
            Kind.ERROR, 1011, "Method call: Attempted to call method {0} on null context object");

        public static readonly SpelMessage CANNOT_INDEX_INTO_NULL_VALUE = new SpelMessage(
            Kind.ERROR, 1012, "Cannot index into a null value");

        public static readonly SpelMessage NOT_COMPARABLE = new SpelMessage(
            Kind.ERROR, 1013, "Cannot compare instances of {0} and {1}");

        public static readonly SpelMessage INCORRECT_NUMBER_OF_ARGUMENTS_TO_FUNCTION = new SpelMessage(
            Kind.ERROR, 1014, "Incorrect number of arguments for function, {0} supplied but function takes {1}");

        public static readonly SpelMessage INVALID_TYPE_FOR_SELECTION = new SpelMessage(
            Kind.ERROR, 1015, "Cannot perform selection on input data of type ''{0}''");

        public static readonly SpelMessage RESULT_OF_SELECTION_CRITERIA_IS_NOT_BOOLEAN = new SpelMessage(
            Kind.ERROR, 1016, "Result of selection criteria is not boolean");

        public static readonly SpelMessage BETWEEN_RIGHT_OPERAND_MUST_BE_TWO_ELEMENT_LIST = new SpelMessage(
            Kind.ERROR, 1017, "Right operand for the 'between' operator has to be a two-element list");

        public static readonly SpelMessage INVALID_PATTERN = new SpelMessage(
            Kind.ERROR, 1018, "Pattern is not valid ''{0}''");

        public static readonly SpelMessage PROJECTION_NOT_SUPPORTED_ON_TYPE = new SpelMessage(
            Kind.ERROR, 1019, "Projection is not supported on the type ''{0}''");

        public static readonly SpelMessage ARGLIST_SHOULD_NOT_BE_EVALUATED = new SpelMessage(
            Kind.ERROR, 1020, "The argument list of a lambda expression should never have getValue() called upon it");

        public static readonly SpelMessage EXCEPTION_DURING_PROPERTY_READ = new SpelMessage(
            Kind.ERROR, 1021, "A problem occurred whilst attempting to access the property ''{0}'': ''{1}''");

        public static readonly SpelMessage FUNCTION_REFERENCE_CANNOT_BE_INVOKED = new SpelMessage(
            Kind.ERROR, 1022, "The function ''{0}'' mapped to an object of type ''{1}'' which cannot be invoked");

        public static readonly SpelMessage EXCEPTION_DURING_FUNCTION_CALL = new SpelMessage(
            Kind.ERROR, 1023, "A problem occurred whilst attempting to invoke the function ''{0}'': ''{1}''");

        public static readonly SpelMessage ARRAY_INDEX_OUT_OF_BOUNDS = new SpelMessage(
            Kind.ERROR, 1024, "The array has ''{0}'' elements, index ''{1}'' is invalid");

        public static readonly SpelMessage COLLECTION_INDEX_OUT_OF_BOUNDS = new SpelMessage(
            Kind.ERROR, 1025, "The collection has ''{0}'' elements, index ''{1}'' is invalid");

        public static readonly SpelMessage STRING_INDEX_OUT_OF_BOUNDS = new SpelMessage(
            Kind.ERROR, 1026, "The string has ''{0}'' characters, index ''{1}'' is invalid");

        public static readonly SpelMessage INDEXING_NOT_SUPPORTED_FOR_TYPE = new SpelMessage(
            Kind.ERROR, 1027, "Indexing into type ''{0}'' is not supported");

        public static readonly SpelMessage INSTANCEOF_OPERATOR_NEEDS_CLASS_OPERAND = new SpelMessage(
            Kind.ERROR, 1028, "The operator 'instanceof' needs the right operand to be a class, not a ''{0}''");

        public static readonly SpelMessage EXCEPTION_DURING_METHOD_INVOCATION = new SpelMessage(
            Kind.ERROR, 1029, "A problem occurred when trying to execute method ''{0}'' on object of type ''{1}'': ''{2}''");

        public static readonly SpelMessage OPERATOR_NOT_SUPPORTED_BETWEEN_TYPES = new SpelMessage(
            Kind.ERROR, 1030, "The operator ''{0}'' is not supported between objects of type ''{1}'' and ''{2}''");

        public static readonly SpelMessage PROBLEM_LOCATING_METHOD = new SpelMessage(
            Kind.ERROR, 1031, "Problem locating method {0} on type {1}");

        public static readonly SpelMessage SETVALUE_NOT_SUPPORTED = new SpelMessage(
            Kind.ERROR, 1032, "setValue(ExpressionState, Object) not supported for ''{0}''");

        public static readonly SpelMessage MULTIPLE_POSSIBLE_METHODS = new SpelMessage(
            Kind.ERROR, 1033, "Method call of ''{0}'' is ambiguous, supported type conversions allow multiple variants to match");

        public static readonly SpelMessage EXCEPTION_DURING_PROPERTY_WRITE = new SpelMessage(
            Kind.ERROR, 1034, "A problem occurred whilst attempting to set the property ''{0}'': {1}");

        public static readonly SpelMessage NOT_AN_INTEGER = new SpelMessage(
            Kind.ERROR, 1035, "The value ''{0}'' cannot be parsed as an int");

        public static readonly SpelMessage NOT_A_LONG = new SpelMessage(
            Kind.ERROR, 1036, "The value ''{0}'' cannot be parsed as a long");

        public static readonly SpelMessage INVALID_FIRST_OPERAND_FOR_MATCHES_OPERATOR = new SpelMessage(
            Kind.ERROR, 1037, "First operand to matches operator must be a string. ''{0}'' is not");

        public static readonly SpelMessage INVALID_SECOND_OPERAND_FOR_MATCHES_OPERATOR = new SpelMessage(
            Kind.ERROR, 1038, "Second operand to matches operator must be a string. ''{0}'' is not");

        public static readonly SpelMessage FUNCTION_MUST_BE_STATIC = new SpelMessage(
            Kind.ERROR, 1039, "Only static methods can be called via function references. The method ''{0}'' referred to by name ''{1}'' is not static.");

        public static readonly SpelMessage NOT_A_REAL = new SpelMessage(
            Kind.ERROR, 1040, "The value ''{0}'' cannot be parsed as a double");

        public static readonly SpelMessage MORE_INPUT = new SpelMessage(
            Kind.ERROR, 1041, "After parsing a valid expression, there is still more data in the expression: ''{0}''");

        public static readonly SpelMessage RIGHT_OPERAND_PROBLEM = new SpelMessage(
            Kind.ERROR, 1042, "Problem parsing right operand");

        public static readonly SpelMessage NOT_EXPECTED_TOKEN = new SpelMessage(
            Kind.ERROR, 1043, "Unexpected token. Expected ''{0}'' but was ''{1}''");

        public static readonly SpelMessage OOD = new SpelMessage(
            Kind.ERROR, 1044, "Unexpectedly ran out of input");

        public static readonly SpelMessage NON_TERMINATING_DOUBLE_QUOTED_STRING = new SpelMessage(
            Kind.ERROR, 1045, "Cannot find terminating \" for string");

        public static readonly SpelMessage NON_TERMINATING_QUOTED_STRING = new SpelMessage(
            Kind.ERROR, 1046, "Cannot find terminating '' for string");

        public static readonly SpelMessage MISSING_LEADING_ZERO_FOR_NUMBER = new SpelMessage(
            Kind.ERROR, 1047, "A real number must be prefixed by zero, it cannot start with just ''.''");

        public static readonly SpelMessage REAL_CANNOT_BE_LONG = new SpelMessage(
            Kind.ERROR, 1048, "Real number cannot be suffixed with a long (L or l) suffix");

        public static readonly SpelMessage UNEXPECTED_DATA_AFTER_DOT = new SpelMessage(
            Kind.ERROR, 1049, "Unexpected data after ''.'': ''{0}''");

        public static readonly SpelMessage MISSING_CONSTRUCTOR_ARGS = new SpelMessage(
            Kind.ERROR, 1050, "The arguments '(...)' for the constructor call are missing");

        public static readonly SpelMessage RUN_OUT_OF_ARGUMENTS = new SpelMessage(
            Kind.ERROR, 1051, "Unexpectedly ran out of arguments");

        public static readonly SpelMessage UNABLE_TO_GROW_COLLECTION = new SpelMessage(
            Kind.ERROR, 1052, "Unable to grow collection");

        public static readonly SpelMessage UNABLE_TO_GROW_COLLECTION_UNKNOWN_ELEMENT_TYPE = new SpelMessage(
            Kind.ERROR, 1053, "Unable to grow collection: unable to determine list element type");

        public static readonly SpelMessage UNABLE_TO_CREATE_LIST_FOR_INDEXING = new SpelMessage(
            Kind.ERROR, 1054, "Unable to dynamically create a List to replace a null value");

        public static readonly SpelMessage UNABLE_TO_CREATE_MAP_FOR_INDEXING = new SpelMessage(
            Kind.ERROR, 1055, "Unable to dynamically create a Map to replace a null value");

        public static readonly SpelMessage UNABLE_TO_DYNAMICALLY_CREATE_OBJECT = new SpelMessage(
            Kind.ERROR, 1056, "Unable to dynamically create instance of ''{0}'' to replace a null value");

        public static readonly SpelMessage NO_BEAN_RESOLVER_REGISTERED = new SpelMessage(
            Kind.ERROR, 1057, "No bean resolver registered in the context to resolve access to bean ''{0}''");

        public static readonly SpelMessage EXCEPTION_DURING_BEAN_RESOLUTION = new SpelMessage(
            Kind.ERROR, 1058, "A problem occurred when trying to resolve bean ''{0}'':''{1}''");

        public static readonly SpelMessage INVALID_BEAN_REFERENCE = new SpelMessage(
            Kind.ERROR, 1059, "@ or & can only be followed by an identifier or a quoted name");

        public static readonly SpelMessage TYPE_NAME_EXPECTED_FOR_ARRAY_CONSTRUCTION = new SpelMessage(
            Kind.ERROR, 1060, "Expected the type of the new array to be specified as a String but found ''{0}''");

        public static readonly SpelMessage INCORRECT_ELEMENT_TYPE_FOR_ARRAY = new SpelMessage(
            Kind.ERROR, 1061, "The array of type ''{0}'' cannot have an element of type ''{1}'' inserted");

        public static readonly SpelMessage MULTIDIM_ARRAY_INITIALIZER_NOT_SUPPORTED = new SpelMessage(
            Kind.ERROR, 1062, "Using an initializer to build a multi-dimensional array is not currently supported");

        public static readonly SpelMessage MISSING_ARRAY_DIMENSION = new SpelMessage(
            Kind.ERROR, 1063, "A required array dimension has not been specified");

        public static readonly SpelMessage INITIALIZER_LENGTH_INCORRECT = new SpelMessage(
            Kind.ERROR, 1064, "Array initializer size does not match array dimensions");

        public static readonly SpelMessage UNEXPECTED_ESCAPE_CHAR = new SpelMessage(
            Kind.ERROR, 1065, "Unexpected escape character");

        public static readonly SpelMessage OPERAND_NOT_INCREMENTABLE = new SpelMessage(
            Kind.ERROR, 1066, "The expression component ''{0}'' does not support increment");

        public static readonly SpelMessage OPERAND_NOT_DECREMENTABLE = new SpelMessage(
            Kind.ERROR, 1067, "The expression component ''{0}'' does not support decrement");

        public static readonly SpelMessage NOT_ASSIGNABLE = new SpelMessage(
            Kind.ERROR, 1068, "The expression component ''{0}'' is not assignable");

        public static readonly SpelMessage MISSING_CHARACTER = new SpelMessage(
            Kind.ERROR, 1069, "Missing expected character ''{0}''");

        public static readonly SpelMessage LEFT_OPERAND_PROBLEM = new SpelMessage(
            Kind.ERROR, 1070, "Problem parsing left operand");

        public static readonly SpelMessage MISSING_SELECTION_EXPRESSION = new SpelMessage(
            Kind.ERROR, 1071, "A required selection expression has not been specified");

        public static readonly SpelMessage EXCEPTION_RUNNING_COMPILED_EXPRESSION = new SpelMessage(
            Kind.ERROR, 1072, "An exception occurred whilst evaluating a compiled expression");

        public static readonly SpelMessage FLAWED_PATTERN = new SpelMessage(
            Kind.ERROR, 1073, "Failed to efficiently evaluate pattern ''{0}'': consider redesigning it");

        public enum Kind
        {
            INFO,
            WARNING,
            ERROR
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
                case Kind.ERROR:
                    formattedMessage.Append("E");
                    break;
            }

            formattedMessage.Append(": ");
            formattedMessage.Append(string.Format(_message, inserts));
            return formattedMessage.ToString();
        }

        public override bool Equals(object obj)
        {
            var asMessage = obj as SpelMessage;
            if (asMessage == null)
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
}
