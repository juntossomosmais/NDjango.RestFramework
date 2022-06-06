using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AspNetCore.RestFramework.Core.Serializer;

public class JsonTransform : DefaultContractResolver
{
    private readonly HashSet<string> ignoreProps;

    public JsonTransform(IEnumerable<string> propNamesToIgnore)
    {
        this.ignoreProps = new HashSet<string>(propNamesToIgnore);
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        if (this.ignoreProps.Contains(property.PropertyName))
        {
            property.ShouldSerialize = _ => true;
        }
        else
        {
            property.ShouldSerialize = _ => false;
        }

        return property;
    }
}