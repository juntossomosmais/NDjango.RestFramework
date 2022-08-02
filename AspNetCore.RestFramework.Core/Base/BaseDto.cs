using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Base
{
    public abstract class BaseDto<TPrimaryKey> 
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public TPrimaryKey Id { get; set; }

        public abstract IEnumerable<string> Validate();
    }
}
