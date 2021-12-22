using CSharpRestFramework.Base;
using System;

namespace WebApplication2.Models
{
    public class CustomerDocument
    {
        public Guid Id { get; set; }
        public string Document { get; set; }
        public string DocumentType { get; set; }
        public Guid CustomerId { get; set; }

        public Customer Customer { get; set; }
    }
}
