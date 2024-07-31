// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable S4040 // Strings should be normalized to uppercase

namespace Steeltoe.Common.CasingConventions;

/// <summary>
/// Converts between pascal-cased enum members and their snake-case string representation.
/// </summary>
/// <typeparam name="TEnum">
/// The enumeration type.
/// </typeparam>
internal sealed class SnakeCaseEnumConverter<TEnum>(SnakeCaseStyle style) : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly SnakeCaseStyle _style = style;

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonTokenType token = reader.TokenType;

        if (token == JsonTokenType.String)
        {
            string enumText = reader.GetString()!;
            string pascalCaseText = ToPascalCase(enumText);

            if (Enum.TryParse(pascalCaseText, out TEnum value) || Enum.TryParse(pascalCaseText, true, out value))
            {
                return value;
            }
        }

        throw new JsonException();
    }

    private string ToPascalCase(string snakeCaseText)
    {
        var builder = new StringBuilder();
        bool nextCharToUpper = true;

        foreach (char ch in snakeCaseText)
        {
            if (ch == '_')
            {
                nextCharToUpper = true;
                continue;
            }

            if (nextCharToUpper)
            {
                builder.Append(char.ToUpperInvariant(ch));
                nextCharToUpper = false;
            }
            else
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        string pascalCaseText = value.ToString();
        string snakeCaseText = ToSnakeCase(pascalCaseText);

        writer.WriteStringValue(snakeCaseText);
    }

    public string ToSnakeCase(string pascalCaseText)
    {
        var builder = new StringBuilder();

        for (int index = 0; index < pascalCaseText.Length; index++)
        {
            char ch = pascalCaseText[index];

            if (char.IsUpper(ch))
            {
                if (index > 0)
                {
                    builder.Append('_');
                }

                builder.Append(_style == SnakeCaseStyle.AllCaps ? ch : char.ToLowerInvariant(ch));
            }
            else
            {
                builder.Append(_style == SnakeCaseStyle.NoCaps ? ch : char.ToUpperInvariant(ch));
            }
        }

        return builder.ToString();
    }
}
