using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AspNetCore.RestFramework.Core.Base
{
    public abstract class BaseDto<TType> 
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public TType Id { get; set; }

        public abstract IEnumerable<string> Validate();
    }
}
