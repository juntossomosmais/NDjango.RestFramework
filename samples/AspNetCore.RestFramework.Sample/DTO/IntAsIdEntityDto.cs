using AspNetCore.RestFramework.Core.Base;

namespace AspNetRestFramework.Sample.DTO
{
    public class IntAsIdEntityDto : BaseDto<int>
    {
        public string Name { get; set; }
    }
}
