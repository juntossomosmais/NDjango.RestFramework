using AspNetCore.RestFramework.Core.Base;
using System.Collections.Generic;

namespace AspNetRestFramework.Sample.DTO
{
    public class SellerDto : BaseDto
    {
        public string Name { get; set; }

        public override IEnumerable<string> Validate()
        {
            return new List<string>();
        }
    }
}
