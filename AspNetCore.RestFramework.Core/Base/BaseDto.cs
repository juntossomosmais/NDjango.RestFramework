using System;
using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Base
{
    public abstract class BaseDto
    {
        public Guid Id { get; set; }

        public abstract IEnumerable<string> Validate();
    }
}
