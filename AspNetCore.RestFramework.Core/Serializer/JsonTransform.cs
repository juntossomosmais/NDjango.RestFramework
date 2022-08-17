using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AspNetCore.RestFramework.Core.Serializer;

public class JsonTransform : DefaultContractResolver
{
    private readonly HashSet<string> _propNamesToShow;

    public JsonTransform(IEnumerable<string> propNamesToShow)
    {
        NamingStrategy = new CamelCaseNamingStrategy();
        _propNamesToShow = new HashSet<string>(propNamesToShow);
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        if (_propNamesToShow.Any(m => m.Equals(property.PropertyName, StringComparison.OrdinalIgnoreCase)))
        {
            property.ShouldSerialize = _ => true;
        }
        else
        {
            var namespaceArray = member?.DeclaringType?.ToString().Split(".");
            var className = namespaceArray?.Last();

            if (_propNamesToShow.Any(m => m.Equals($"{className}:{property.PropertyName}", StringComparison.OrdinalIgnoreCase)))
                property.ShouldSerialize = _ => true;
            else
                property.ShouldSerialize = _ => false;
        }
        return property;
    }
}