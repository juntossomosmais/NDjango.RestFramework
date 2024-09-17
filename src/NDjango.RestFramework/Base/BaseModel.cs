namespace NDjango.RestFramework.Base;

public abstract class BaseModel<TPrimaryKey>
{
    public TPrimaryKey Id { get; set; }
    public abstract string[] GetFields();
}