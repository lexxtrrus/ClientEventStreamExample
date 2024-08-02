using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class EventLogsConverter : JsonConverter<Dictionary<string, List<EventData>>>
{
    public override void WriteJson(JsonWriter writer, Dictionary<string, List<EventData>> value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            serializer.Serialize(writer, kvp.Value);
        }

        writer.WriteEndObject();
    }

    public override Dictionary<string, List<EventData>> ReadJson(JsonReader reader, Type objectType, Dictionary<string, List<EventData>> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var result = new Dictionary<string, List<EventData>>();

        JObject jObject = JObject.Load(reader);

        foreach (var property in jObject.Properties())
        {
            var eventDataList = property.Value.ToObject<List<EventData>>(serializer);
            result.Add(property.Name, eventDataList);
        }

        return result;
    }
}