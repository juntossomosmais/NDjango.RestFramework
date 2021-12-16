using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication2.DTO
{
    public abstract class BaseDto
    {
        public Guid Id { get; set; }

        public abstract bool IsValid();
    }
}
