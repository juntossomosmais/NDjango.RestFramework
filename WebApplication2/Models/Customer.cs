using System.Collections.Generic;

namespace WebApplication2.Models
{
    public class Customer : BaseEntity
    {
        public string Name { get; set; }
        public string CNPJ { get; set; }
    }
}
