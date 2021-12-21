using CSharpRestFramework.Base;
using System.Collections.Generic;

namespace WebApplication2.DTO
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
