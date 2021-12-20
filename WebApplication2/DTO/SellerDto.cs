using CSharpRestFramework.Base;

namespace WebApplication2.DTO
{
    public class SellerDto : BaseDto
    {
        public string Name { get; set; }

        public override bool IsValid()
        {
            return true;
        }
    }
}
