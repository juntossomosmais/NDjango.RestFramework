using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Base
{
    public class PagedBaseResponse<T>
    {
        public int Pages { get; set; }
        public T Data { get; set; }
    }
}
