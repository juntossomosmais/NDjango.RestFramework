namespace AspNetCore.RestFramework.Core.Base;

public class ActionOptions
{
    public bool AllowList { get; set; } = true;
    public bool AllowPost { get; set; } = true;
    public bool AllowPatch { get; set; } = true;
    public bool AllowPut { get; set; } = true;
}