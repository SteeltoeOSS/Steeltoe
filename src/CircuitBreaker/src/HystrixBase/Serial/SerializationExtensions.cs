// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial;

public static class SerializationExtensions
{
    public static void WriteStringField(this JsonTextWriter writer, string fieldName, string fieldValue)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteValue(fieldValue);
    }

    public static void WriteLongField(this JsonTextWriter writer, string fieldName, long fieldValue)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteValue(fieldValue);
    }

    public static void WriteBooleanField(this JsonTextWriter writer, string fieldName, bool fieldValue)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteValue(fieldValue);
    }

    public static void WriteIntegerField(this JsonTextWriter writer, string fieldName, int fieldValue)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteValue(fieldValue);
    }

    public static void WriteObjectFieldStart(this JsonTextWriter writer, string fieldName)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
    }

    public static void WriteArrayFieldStart(this JsonTextWriter writer, string fieldName)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartArray();
    }
}