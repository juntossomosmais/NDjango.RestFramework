using System.Collections.Generic;

namespace CSharpRestFramework.Base
{
    public class PagedBaseResponse<T>
    {
        public int Pages { get; set; }
        public List<T> Data { get; set; }
    }
}
