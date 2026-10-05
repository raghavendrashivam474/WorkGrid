using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkGrid.Domain.Sync;

namespace WorkGrid.Infrastructure.Remote.Sync;

public sealed class SyncObjectKeyJsonConverter : JsonConverter<SyncObjectKey>
{
    public override SyncObjectKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (string.IsNullOrWhiteSpace(str))
            throw new JsonException("SyncObjectKey string cannot be null or empty.");

        return Parse(str);
    }

    public override void Write(Utf8JsonWriter writer, SyncObjectKey value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(Format(value));
    }

    public override SyncObjectKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (string.IsNullOrWhiteSpace(str))
            throw new JsonException("SyncObjectKey property name cannot be null or empty.");

        return Parse(str);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, SyncObjectKey value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WritePropertyName(Format(value));
    }

    private static string Format(SyncObjectKey key) => $"{key.Type}:{key.Id}";

    private static SyncObjectKey Parse(string str)
    {
        var parts = str.Split(':');
        if (parts.Length != 2 || !Enum.TryParse<SyncObjectType>(parts[0], out var type) || !Guid.TryParse(parts[1], out var id))
        {
            throw new JsonException($"Invalid SyncObjectKey format: '{str}'");
        }

        return new SyncObjectKey(type, id);
    }
}
