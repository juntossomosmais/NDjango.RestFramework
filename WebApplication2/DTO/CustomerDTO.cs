using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication2.DTO
{
    public class CustomerDTO : BaseDto
    {
        public CustomerDTO()
        {
            
        }

        public string Name { get; set; }
        public string CNPJ { get; set; }

        public override bool IsValid()
        {
            return true;
        }
    }
}
