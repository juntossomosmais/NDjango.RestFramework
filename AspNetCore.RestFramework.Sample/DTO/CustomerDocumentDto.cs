using System;
using AspNetCore.RestFramework.Core.Base;

namespace AspNetRestFramework.Sample.DTO;

public class CustomerDocumentDto : BaseDto<Guid>
{
    public string Document { get; set; }
    public string DocumentType { get; set; }
}