using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Base
{
    public class PagedBaseResponse<TData>
    {
        public int Total { get; set; }
        public TData Data { get; set; }
    }
}
