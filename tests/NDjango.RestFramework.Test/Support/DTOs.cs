using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NDjango.RestFramework.Base;

namespace NDjango.RestFramework.Test.Support;

public class CustomerDocumentDto : BaseDto<Guid>
{
    [MinLength(3, ErrorMessage = "Name should have at least 3 characters")]
    public string Document { get; set; }
    public string DocumentType { get; set; }
}

public class CustomerDto : BaseDto<Guid>
{
    public CustomerDto()
    {
    }

    [MinLength(3, ErrorMessage = "Name should have at least 3 characters")]
    public string Name { get; set; }
    public string CNPJ { get; set; }

    public ICollection<CustomerDocumentDto> CustomerDocuments { get; set; }
}

/// <summary>
/// CustomerDto variant that carries a property absent on <see cref="Customer"/>. Used to
/// pin <c>PartialUpdateAsync</c>'s DRF-parity behavior of silently skipping origin fields
/// that have no matching destination property.
/// </summary>
public class CustomerWithUnknownFieldDto : BaseDto<Guid>
{
    public string? Name { get; set; }
    public string? CNPJ { get; set; }
    public string? SecretSauce { get; set; }
}

public class IntAsIdEntityDto : BaseDto<int>
{
    public string Name { get; set; }
}

public class SellerDto : BaseDto<Guid>
{
    public string Name { get; set; }
}

public class InvalidFieldEntityDto : BaseDto<Guid>
{
    public string Name { get; set; }
}
