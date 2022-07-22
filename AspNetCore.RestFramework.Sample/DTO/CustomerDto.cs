using System;
using AspNetCore.RestFramework.Core.Base;
using System.Collections.Generic;

namespace AspNetRestFramework.Sample.DTO
{
    public class CustomerDTO : BaseDto<Guid>
    {
        public CustomerDTO()
        {

        }

        public string Name { get; set; }
        public string CNPJ { get; set; }
        
        public ICollection<CustomerDocumentsDTO> CustomerDocuments { get; set; }

        public override IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            if (Name.Length < 3)
                errors.Add("Name should have at least 3 chars");

            return errors;
        }
    }
}