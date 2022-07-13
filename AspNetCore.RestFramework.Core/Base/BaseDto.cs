using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AspNetCore.RestFramework.Core.Base
{
    public abstract class BaseDto
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid Id { get; set; }

        public abstract IEnumerable<string> Validate();
    }
}
