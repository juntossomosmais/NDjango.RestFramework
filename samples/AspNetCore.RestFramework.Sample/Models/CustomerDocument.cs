using AspNetCore.RestFramework.Core.Base;
using System;

namespace AspNetRestFramework.Sample.Models
{
    public class CustomerDocument : BaseModel<Guid>
    {
        public string Document { get; set; }
        public string DocumentType { get; set; }
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }
        public override string[] GetFields()
        {
            return new[] { "Id", "Document", "DocumentType", "CustomerId", "Customer", "Customer:CNPJ", "Customer:Age" };
        }
    }
}
