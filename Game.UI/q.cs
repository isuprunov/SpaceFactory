using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Client;

public partial class GameClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.Converters.Add(new AnswerJsonConverter());
    }
}

public class AnswerJsonConverter : JsonConverter<Answer>
{
    private static readonly Dictionary<string, Type> _typeMap;

    static AnswerJsonConverter()
    {
        var baseType = typeof(Answer);
        _typeMap = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t => baseType.IsAssignableFrom(t) && t != baseType && !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    public override Answer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("Missing 'type' discriminator");

        var typeDiscriminator = typeProp.GetString();
        if (typeDiscriminator == null || !_typeMap.TryGetValue(typeDiscriminator, out var targetType))
            throw new JsonException($"Unknown type discriminator: {typeDiscriminator}");

        var json = root.GetRawText();
        return (Answer?)JsonSerializer.Deserialize(json, targetType, options);
    }

    public override void Write(Utf8JsonWriter writer, Answer value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}