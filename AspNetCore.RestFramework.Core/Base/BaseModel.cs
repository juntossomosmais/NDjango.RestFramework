using System;

namespace AspNetCore.RestFramework.Core.Base;

public abstract class BaseModel<TPrimaryKey>
{
    public TPrimaryKey Id { get; set; }
    public abstract string[] GetFields();
}