using System;
using System.Collections.Generic;
using AspNetCore.RestFramework.Core.Base;

namespace AspNetCore.RestFramework.Core.Test.Support;

public class Customer : BaseModel<Guid>
{
    public string Name { get; set; }
    public string CNPJ { get; set; }
    public int Age { get; set; }

    public ICollection<CustomerDocument> CustomerDocument { get; set; }

    public override string[] GetFields()
    {
        return new[] { "Name", "CNPJ", "Age", "Id", "CustomerDocument", "CustomerDocument:DocumentType", "CustomerDocument:Document" };
    }
}

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

public class IntAsIdEntity : BaseModel<int>
{
    public string Name { get; set; }

    public override string[] GetFields()
        => new[] { nameof(Id), nameof(Name) };
}

public class Seller : BaseModel<Guid>
{
    public string Name { get; set; }
    public override string[] GetFields() => throw new NotImplementedException();
}
