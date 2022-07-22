using System;

namespace AspNetCore.RestFramework.Core.Base;

public abstract class BaseModel<TType> 
{
    public TType Id { get; set; }
    public abstract string[] GetFields();
}