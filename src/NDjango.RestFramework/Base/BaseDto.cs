namespace NDjango.RestFramework.Base
{
    public abstract class BaseDto<TPrimaryKey>
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public TPrimaryKey Id { get; set; }
    }
}
