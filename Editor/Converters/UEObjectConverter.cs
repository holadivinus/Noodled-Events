#if UNITY_EDITOR
using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

public class UEObjectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(UnityEngine.Object).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        string globalIdStr = (string)JToken.Load(reader);
        if (string.IsNullOrEmpty(globalIdStr))
            return null;

        GlobalObjectId globalId;
        if (GlobalObjectId.TryParse(globalIdStr, out globalId))
        {
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object existingValue, JsonSerializer serializer)
    {
        UnityEngine.Object obj = existingValue as UnityEngine.Object;
        if (obj == null)
        {
            writer.WriteNull();
            return;
        }

        GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
        writer.WriteValue(globalId.ToString());
    }
}
#endif