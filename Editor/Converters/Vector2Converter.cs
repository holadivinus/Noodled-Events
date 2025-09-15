#if UNITY_EDITOR
using System;
using UnityEngine;
using Newtonsoft.Json;

public class Vector2Converter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector2);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            float x = 0f;
            float y = 0f;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)reader.Value;
                    reader.Read();

                    if (propertyName == "x")
                        x = Convert.ToSingle(reader.Value);
                    else if (propertyName == "y")
                        y = Convert.ToSingle(reader.Value);
                }
            }

            return new Vector2(x, y);
        }

        throw new JsonSerializationException("Unexpected token when deserializing Vector2.");
    }

    public override void WriteJson(JsonWriter writer, object existingValue, JsonSerializer serializer)
    {
        Vector2 vector = (Vector2)existingValue;

        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(vector.x);
        writer.WritePropertyName("y");
        writer.WriteValue(vector.y);
        writer.WriteEndObject();
    }
}
#endif