using System;
using System.Collections.Generic;
using AspNetCore.RestFramework.Core.Base;

namespace AspNetCore.RestFramework.Core.Test.Support;

public class CustomerDocumentDto : BaseDto<Guid>
{
    public string Document { get; set; }
    public string DocumentType { get; set; }
}

public class CustomerDto : BaseDto<Guid>
{
    public CustomerDto()
    {
    }

    public string Name { get; set; }
    public string CNPJ { get; set; }

    public ICollection<CustomerDocumentDto> CustomerDocuments { get; set; }
}

public class IntAsIdEntityDto : BaseDto<int>
{
    public string Name { get; set; }
}

public class SellerDto : BaseDto<Guid>
{
    public string Name { get; set; }
}
