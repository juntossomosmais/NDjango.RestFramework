using System.Collections.Generic;
using AspNetCore.RestFramework.Core.Base;

namespace AspNetRestFramework.Sample.DTO;

public class CustomerDocumentsDTO : BaseDto
{
    public string Document { get; set; }
    public string DocumentType { get; set; }

    public override IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (Document.Length < 3)
            errors.Add("Name should have at least 3 chars");

        return errors;
    }
}