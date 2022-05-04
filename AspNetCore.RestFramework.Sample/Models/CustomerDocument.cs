using AspNetCore.RestFramework.Core.Base;
using System;

namespace AspNetRestFramework.Sample.Models
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
