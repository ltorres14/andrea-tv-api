using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AndreaTV.Api.Infrastructure.Xtream.Serialization;

public sealed class FlexibleBooleanJsonConverter : JsonConverter<bool?>
{
    public override bool? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }

        if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }

        if (reader.TokenType == JsonTokenType.Number
            && reader.TryGetInt64(out var number))
        {
            return number != 0;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (bool.TryParse(value, out var boolean))
            {
                return boolean;
            }

            if (long.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out number))
            {
                return number != 0;
            }
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        bool? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteBooleanValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public sealed class FlexibleInt32JsonConverter : JsonConverter<int?>
{
    public override int? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number
            && reader.TryGetInt32(out var number))
        {
            return number;
        }

        if (reader.TokenType == JsonTokenType.String
            && int.TryParse(
                reader.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out number))
        {
            return number;
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        int? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public sealed class FlexibleInt64JsonConverter : JsonConverter<long?>
{
    public override long? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number
            && reader.TryGetInt64(out var number))
        {
            return number;
        }

        if (reader.TokenType == JsonTokenType.String
            && long.TryParse(
                reader.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out number))
        {
            return number;
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        long? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => GetNumberAsString(ref reader),
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            _ => null
        };

    public override void Write(
        Utf8JsonWriter writer,
        string? value,
        JsonSerializerOptions options) => writer.WriteStringValue(value);

    private static string GetNumberAsString(ref Utf8JsonReader reader)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.GetRawText();
    }
}

public sealed class FlexibleStringListJsonConverter
    : JsonConverter<IReadOnlyList<string>?>
{
    public override IReadOnlyList<string>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return string.IsNullOrWhiteSpace(value)
                ? []
                : [value];
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            using var ignoredValue = JsonDocument.ParseValue(ref reader);
            return [];
        }

        var values = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
            else
            {
                using var ignoredValue = JsonDocument.ParseValue(ref reader);
            }
        }

        return values;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<string>? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }
}
