#if UNITY_EDITOR
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class UnityStructConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector2) ||
               objectType == typeof(Vector3) ||
               objectType == typeof(Vector4) ||
               objectType == typeof(Quaternion) ||
               objectType == typeof(Color) ||
               objectType == typeof(Color32);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (value is Vector2 v2)
        {
            writer.WritePropertyName("_type"); writer.WriteValue("Vector2");
            writer.WritePropertyName("x"); writer.WriteValue(v2.x);
            writer.WritePropertyName("y"); writer.WriteValue(v2.y);
        }
        else if (value is Vector3 v3)
        {
            writer.WritePropertyName("_type"); writer.WriteValue("Vector3");
            writer.WritePropertyName("x"); writer.WriteValue(v3.x);
            writer.WritePropertyName("y"); writer.WriteValue(v3.y);
            writer.WritePropertyName("z"); writer.WriteValue(v3.z);
        }
        else if (value is Vector4 v4)
        {
            writer.WritePropertyName("_type"); writer.WriteValue("Vector4");
            writer.WritePropertyName("x"); writer.WriteValue(v4.x);
            writer.WritePropertyName("y"); writer.WriteValue(v4.y);
            writer.WritePropertyName("z"); writer.WriteValue(v4.z);
            writer.WritePropertyName("w"); writer.WriteValue(v4.w);
        }
        else if (value is Quaternion q)
        {
            writer.WritePropertyName("_type"); writer.WriteValue("Quaternion");
            writer.WritePropertyName("x"); writer.WriteValue(q.x);
            writer.WritePropertyName("y"); writer.WriteValue(q.y);
            writer.WritePropertyName("z"); writer.WriteValue(q.z);
            writer.WritePropertyName("w"); writer.WriteValue(q.w);
        }
        else if (value is Color c)
        {
            writer.WritePropertyName("_type"); writer.WriteValue("Color");
            writer.WritePropertyName("r"); writer.WriteValue(c.r);
            writer.WritePropertyName("g"); writer.WriteValue(c.g);
            writer.WritePropertyName("b"); writer.WriteValue(c.b);
            writer.WritePropertyName("a"); writer.WriteValue(c.a);
        }
        else if (value is Color32 c32)
        {
            writer.WritePropertyName("_type"); writer.WriteValue("Color32");
            writer.WritePropertyName("r"); writer.WriteValue(c32.r);
            writer.WritePropertyName("g"); writer.WriteValue(c32.g);
            writer.WritePropertyName("b"); writer.WriteValue(c32.b);
            writer.WritePropertyName("a"); writer.WriteValue(c32.a);
        }

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        string type = obj["_type"]?.ToString();
        if (string.IsNullOrEmpty(type))
            throw new JsonSerializationException("Missing _type for Unity struct");

        return type switch
        {
            "Vector2" => new Vector2(
                                obj["x"]?.ToObject<float>() ?? 0f,
                                obj["y"]?.ToObject<float>() ?? 0f
                            ),
            "Vector3" => new Vector3(
                                obj["x"]?.ToObject<float>() ?? 0f,
                                obj["y"]?.ToObject<float>() ?? 0f,
                                obj["z"]?.ToObject<float>() ?? 0f
                            ),
            "Vector4" => new Vector4(
                                obj["x"]?.ToObject<float>() ?? 0f,
                                obj["y"]?.ToObject<float>() ?? 0f,
                                obj["z"]?.ToObject<float>() ?? 0f,
                                obj["w"]?.ToObject<float>() ?? 0f
                            ),
            "Quaternion" => new Quaternion(
                                obj["x"]?.ToObject<float>() ?? 0f,
                                obj["y"]?.ToObject<float>() ?? 0f,
                                obj["z"]?.ToObject<float>() ?? 0f,
                                obj["w"]?.ToObject<float>() ?? 0f
                            ),
            "Color" => new Color(
                                obj["r"]?.ToObject<float>() ?? 0f,
                                obj["g"]?.ToObject<float>() ?? 0f,
                                obj["b"]?.ToObject<float>() ?? 0f,
                                obj["a"]?.ToObject<float>() ?? 1f
                            ),
            "Color32" => new Color32(
                                obj["r"]?.ToObject<byte>() ?? 0,
                                obj["g"]?.ToObject<byte>() ?? 0,
                                obj["b"]?.ToObject<byte>() ?? 0,
                                obj["a"]?.ToObject<byte>() ?? 255
                            ),
            _ => throw new JsonSerializationException($"Unknown Unity struct type: {type}"),
        };
    }
}

// handles conversion of System.Object to Unity structs using the above converter
public class UnityObjectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(object);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            var jObj = JObject.Load(reader);

            // If it has a _type, try UnityStructConverter
            if (jObj.TryGetValue("_type", out var typeToken))
            {
                string type = typeToken.ToString();
                switch (type)
                {
                    case "Vector2": return jObj.ToObject<Vector2>(serializer);
                    case "Vector3": return jObj.ToObject<Vector3>(serializer);
                    case "Vector4": return jObj.ToObject<Vector4>(serializer);
                    case "Quaternion": return jObj.ToObject<Quaternion>(serializer);
                    case "Color": return jObj.ToObject<Color>(serializer);
                    case "Color32": return jObj.ToObject<Color32>(serializer);
                }
            }

            // fallback: return JObject
            return jObj;
        }

        // if we don't know, let it be handled by the default serializer
        return serializer.Deserialize(reader);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // just let the real UnityStructConverter handle these
        serializer.Serialize(writer, value);
    }
}
#endif