using System.Linq;
using AspNetCore.RestFramework.Core.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.RestFramework.Core.Base;

public class ActionOptions
{
    public bool AllowList { get; set; } = true;
    public bool AllowPost { get; set; } = true;
    public bool AllowPatch { get; set; } = true;
    public bool AllowPut { get; set; } = true;
}