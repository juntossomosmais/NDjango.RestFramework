namespace NDjango.RestFramework.Base;

public abstract class BaseModel<TPrimaryKey>
{
    public TPrimaryKey Id { get; set; }
    /// <summary>
    /// Fields that should be returned by the serializer.
    /// </summary>
    public abstract string[] GetFields();
}
