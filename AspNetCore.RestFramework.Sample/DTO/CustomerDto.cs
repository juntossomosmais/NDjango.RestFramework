using System;
using AspNetCore.RestFramework.Core.Base;
using System.Collections.Generic;

namespace AspNetRestFramework.Sample.DTO
{
    public class CustomerDto : BaseDto<Guid>
    {
        public CustomerDto()
        {
        }

        public string Name { get; set; }
        public string CNPJ { get; set; }
        
        public ICollection<CustomerDocumentDto> CustomerDocuments { get; set; }
    }
}