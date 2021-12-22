using CSharpRestFramework.Base;
using System;
using System.Collections.Generic;

namespace WebApplication2.Models
{
    public class Customer 
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string CNPJ { get; set; }
        public int Age { get; set; }

        public ICollection<CustomerDocument> CustomerDocuments { get; set; }
    }
}
