using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSharpRestFramework.Base
{
    public abstract class BaseDto
    {
        public Guid Id { get; set; }

        public abstract bool IsValid();
    }
}
