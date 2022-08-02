using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AspNetCore.RestFramework.Core.Serializer;

public class JsonTransform : DefaultContractResolver
{
    private readonly HashSet<string> showProps;

    public JsonTransform(IEnumerable<string> propNamesToShow)
    {
        this.showProps = new HashSet<string>(propNamesToShow);
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        if (this.showProps.Contains(property.PropertyName))
        {
            property.ShouldSerialize = _ => true;
        }
        else
        {
            var namespaceArray = member?.DeclaringType?.ToString().Split(".");
            var className = namespaceArray?.Last();

            if (this.showProps.Contains($"{className}:{property.PropertyName}"))
            {
                property.ShouldSerialize = _ => true;
            }
            else
            {
                property.ShouldSerialize = _ => false;
            }
        }
        return property;
    }
}