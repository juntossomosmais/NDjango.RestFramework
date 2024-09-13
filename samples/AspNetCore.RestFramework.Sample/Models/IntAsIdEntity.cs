using AspNetCore.RestFramework.Core.Base;

namespace AspNetRestFramework.Sample.Models
{
    public class IntAsIdEntity : BaseModel<int>
    {
        public string Name { get; set; }

        public override string[] GetFields()
            => new[] { nameof(Id), nameof(Name) };
    }
}
