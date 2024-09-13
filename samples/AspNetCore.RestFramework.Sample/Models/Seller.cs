using AspNetCore.RestFramework.Core.Base;
using System;

namespace AspNetRestFramework.Sample.Models
{
    public class Seller : BaseModel<Guid>
    {
        public string Name { get; set; }
        public override string[] GetFields() => throw new NotImplementedException();
    }
}
