using AspNetCore.RestFramework.Core.Base;
using System;
using System.Collections.Generic;

namespace AspNetRestFramework.Sample.Models
{
    public class Customer : BaseModel<Guid>
    {
        public string Name { get; set; }
        public string CNPJ { get; set; }
        public int Age { get; set; }

        public ICollection<CustomerDocument> CustomerDocuments { get; set; }
        public override string[] GetFields()
        {
            return new[] {"Name"};
        }
    }
}
