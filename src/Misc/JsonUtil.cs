/* Test for directly serializing and deserializing Godot Objects like Nodes. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace WolfUI.Misc;

public interface IRestorable<out T>
{
    public static abstract T Restore();
}

public sealed class OptInJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        List<JsonPropertyInfo> propToRemove = [.. jsonTypeInfo.Properties
                .Where(prop => prop.AttributeProvider is not null 
                               && !prop.AttributeProvider.IsDefined(typeof(JsonIncludeAttribute), false))];

        foreach (var prop in propToRemove)
        {
            jsonTypeInfo.Properties.Remove(prop);
        }

        return jsonTypeInfo;
    }
}

public class StaticFactoryConverter<T> : JsonConverter<T>
{
    private const string FactoryMethodName = "Restore";
    private readonly MethodInfo _factoryMethod = typeof(T).GetMethod(FactoryMethodName, BindingFlags.Public | BindingFlags.Static)
                                                 ?? throw new ArgumentException($"No static method named {FactoryMethodName} found in type {typeof(T).Name}");

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        
        var obj = (T?)_factoryMethod.Invoke(null, []);
        if (obj is null) throw new JsonException($"Can't Invoke Factory Method: {_factoryMethod.Name}");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return obj;
            
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            
            var propertyName = reader.GetString();

            var property = typeof(T).GetTypeInfo().DeclaredProperties
                    .Where(prop => prop.GetCustomAttribute<JsonIncludeAttribute>() is not null) 
                    .Where(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>() is not null)
                    .FirstOrDefault(prop => prop.GetCustomAttributesData()
                        .FirstOrDefault(cad => cad.AttributeType == typeof(JsonPropertyNameAttribute))?.ConstructorArguments
                            .First().Value as string == propertyName);
            
            reader.Read();
            if(property is null) continue;
            dynamic? value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
            property.SetValue(obj, value);
            
        }
        throw new JsonException();
    }
    
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}