// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring
{
    public class CodeFlow
    {
        private DynamicMethod _dynamicMethod;

        public CodeFlow(DynamicMethod dynamicMethod)
        {
            _dynamicMethod = dynamicMethod;
        }

        public static string ToDescriptor(Type type)
        {
            if (type.IsPrimitive)
            {
                var name = type.Name;
                switch (name.Length)
                {
                    case 4:
                        if (name.Equals("Byte"))
                        {
                            return "B";
                        }
                        else if (name.Equals("Char"))
                        {
                            return "C";
                        }

                        break;
                    case 5:
                        if (name.Equals("Single"))
                        {
                            return "F";
                        }
                        else if (name.Equals("Int16"))
                        {
                            return "S";
                        }
                        else if (name.Equals("Int32"))
                        {
                            return "I";
                        }
                        else if (name.Equals("SByte"))
                        {
                            return "SB";
                        }
                        else if (name.Equals("Int64"))
                        {
                            return "J";
                        }

                        break;
                    case 6:
                        if (name.Equals("Double"))
                        {
                            return "D";
                        }
                        else if (name.Equals("UInt64"))
                        {
                            return "UJ";
                        }
                        else if (name.Equals("UInt32"))
                        {
                            return "UI";
                        }
                        else if (name.Equals("UInt16"))
                        {
                            return "US";
                        }
                        else if (name.Equals("IntPtr"))
                        {
                            return "P";
                        }

                        break;
                    case 7:
                        if (name.Equals("Boolean"))
                        {
                            return "Z";
                        }
                        else if (name.Equals("UIntPtr"))
                        {
                            return "UP";
                        }

                        break;
                }
            }
            else
            {
                if (type.Name.Equals("Void"))
                {
                    return "V";
                }

                if (!type.IsArray)
                {
                    return "L" + type.FullName.Replace('.', '/');
                }
                else
                {
                    return "[" + ToDescriptor(type.GetElementType());
                }
            }

            return string.Empty;
        }

        public static string ToDescriptorFromObject(object value)
        {
            if (value == null)
            {
                return "LSystem/Object";
            }
            else
            {
                return ToDescriptor(value.GetType());
            }
        }

        public static bool IsIntegerForNumericOp(object number)
        {
            // TODO: Could be more here
            // TODO: Look at need to add support for .NET types not present in Java, e.g. ulong, ushort, byte, uint
            return number is int || number is short || number is byte;
        }

        public static bool IsBooleanCompatible(string descriptor)
        {
            return descriptor != null && (descriptor.Equals("Z") || descriptor.Equals("LSystem/Boolean"));
        }

        public static bool IsPrimitive(string descriptor)
        {
            return descriptor != null && (descriptor.Length == 1 || descriptor.Length == 2);
        }

        public static bool AreBoxingCompatible(string desc1, string desc2)
        {
            if (desc1.Equals(desc2))
            {
                return true;
            }

            if (desc1.Length == 1)
            {
                if (desc1.Equals("Z"))
                {
                    return desc2.Equals("System/Boolean");
                }
                else if (desc1.Equals("C"))
                {
                    return desc2.Equals("System/Char");
                }
                else if (desc1.Equals("B"))
                {
                    return desc2.Equals("System/Byte");
                }
                else if (desc1.Equals("D"))
                {
                    return desc2.Equals("LSystem/Double");
                }
                else if (desc1.Equals("F"))
                {
                    return desc2.Equals("LSystem/Single");
                }
                else if (desc1.Equals("S"))
                {
                    return desc2.Equals("LSystem/Int16");
                }
                else if (desc1.Equals("I"))
                {
                    return desc2.Equals("LSystem/Int32");
                }
                else if (desc1.Equals("J"))
                {
                    return desc2.Equals("LSystem/Int64");
                }
                else if (desc1.Equals("P"))
                {
                    return desc2.Equals("LSystem/IntPtr");
                }
            }
            else if (desc2.Length == 1)
            {
                if (desc2.Equals("Z"))
                {
                    return desc1.Equals("LSystem/Boolean");
                }
                else if (desc2.Equals("C"))
                {
                    return desc1.Equals("System/Char");
                }
                else if (desc2.Equals("B"))
                {
                    return desc1.Equals("System/Byte");
                }
                else if (desc2.Equals("D"))
                {
                    return desc1.Equals("LSystem/Double");
                }
                else if (desc2.Equals("F"))
                {
                    return desc1.Equals("LSystem/Single");
                }
                else if (desc2.Equals("S"))
                {
                    return desc1.Equals("LSystem/Int16");
                }
                else if (desc2.Equals("I"))
                {
                    return desc1.Equals("LSystem/Int32");
                }
                else if (desc2.Equals("J"))
                {
                    return desc1.Equals("LSystem/Int64");
                }
                else if (desc2.Equals("P"))
                {
                    return desc1.Equals("LSystem/IntPtr");
                }
            }
            else if (desc1.Length == 2)
            {
                if (desc1.Equals("SB"))
                {
                    return desc2.Equals("LSystem/SByte");
                }
                else if (desc1.Equals("US"))
                {
                    return desc2.Equals("LSystem/UInt16");
                }
                else if (desc1.Equals("UI"))
                {
                    return desc2.Equals("LSystem/UInt32");
                }
                else if (desc1.Equals("UJ"))
                {
                    return desc2.Equals("LSystem/UInt64");
                }
                else if (desc1.Equals("UP"))
                {
                    return desc2.Equals("LSystem/UIntPtr");
                }
            }
            else if (desc2.Length == 2)
            {
                if (desc2.Equals("US"))
                {
                    return desc1.Equals("LSystem/UInt16");
                }
                else if (desc2.Equals("SB"))
                {
                    return desc1.Equals("LSystem/SByte");
                }
                else if (desc2.Equals("UI"))
                {
                    return desc1.Equals("LSystem/UInt32");
                }
                else if (desc2.Equals("UJ"))
                {
                    return desc1.Equals("LSystem/UInt64");
                }
                else if (desc2.Equals("UP"))
                {
                    return desc1.Equals("LSystem/UIntPtr");
                }
            }

            return false;
        }

        public static string ToBoxedDescriptor(string primitiveDescriptor)
        {
            switch (primitiveDescriptor)
            {
                case "I": return "LSystem/Int32";
                case "J": return "LSystem/Int64";
                case "F": return "LSystem/Single";
                case "D": return "LSystem/Double";
                case "B": return "LSystem/Byte";
                case "C": return "LSystem/Char";
                case "S": return "LSystem/Int16";
                case "Z": return "LSystem/Boolean";
                case "P": return "LSystem/IntPtr";
                case "SB": return "LSystem/SByte";
                case "UJ": return "LSystem/UInt64";
                case "UI": return "LSystem/UInt32";
                case "US": return "LSystem/UInt16";
                case "UP": return "LSystem/UIntPtr";
                default:
                    throw new InvalidOperationException("Unexpected non primitive descriptor " + primitiveDescriptor);
            }
        }

        public static string ToPrimitiveTargetDesc(string descriptor)
        {
            if (descriptor.Length == 1 || descriptor.Length == 2)
            {
                return descriptor;
            }
            else if (descriptor.Equals("LSystem/Boolean"))
            {
                return "Z";
            }
            else if (descriptor.Equals("LSystem/Byte"))
            {
                return "B";
            }
            else if (descriptor.Equals("LSystem/Char"))
            {
                return "C";
            }
            else if (descriptor.Equals("LSystem/Double"))
            {
                return "D";
            }
            else if (descriptor.Equals("LSystem/Single"))
            {
                return "F";
            }
            else if (descriptor.Equals("LSystem/Int32"))
            {
                return "I";
            }
            else if (descriptor.Equals("LSystem/Int64"))
            {
                return "J";
            }
            else if (descriptor.Equals("LSystem/Int16"))
            {
                return "S";
            }
            else if (descriptor.Equals("LSystem/SByte"))
            {
                return "SB";
            }
            else if (descriptor.Equals("LSystem/UInt16"))
            {
                return "US";
            }
            else if (descriptor.Equals("LSystem/UInt32"))
            {
                return "UI";
            }
            else if (descriptor.Equals("LSystem/UInt64"))
            {
                return "UJ";
            }
            else if (descriptor.Equals("LSystem/IntPtr"))
            {
                return "P";
            }
            else if (descriptor.Equals("LSystem/UIntPtr"))
            {
                return "UP";
            }
            else
            {
                throw new InvalidOperationException("No primitive for '" + descriptor + "'");
            }
        }

        public static bool IsPrimitiveOrUnboxableSupportedNumberOrBoolean(string descriptor)
        {
            if (descriptor == null)
            {
                return false;
            }

            if (IsPrimitiveOrUnboxableSupportedNumber(descriptor))
            {
                return true;
            }

            return "Z".Equals(descriptor) || descriptor.Equals("LSystem/Boolean");
        }

        public static bool IsPrimitiveOrUnboxableSupportedNumber(string descriptor)
        {
            if (descriptor == null)
            {
                return false;
            }

            // TODO: Look at need to add support for .NET types not present in Java, e.g. ulong, ushort, byte, uint
            if (descriptor.Length == 1)
            {
                return "DFIJ".Contains(descriptor);
            }

            if (descriptor.StartsWith("LSystem/"))
            {
                var name = descriptor.Substring("LSystem/".Length);
                if (name.Equals("Double") || name.Equals("Single") || name.Equals("Int32") || name.Equals("Int64"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
