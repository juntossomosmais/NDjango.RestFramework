using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSharpRestFramework.Base
{
    public class PagedBaseResponse<T>
    {
        public int Pages { get; set; }
        public List<T> Data { get; set; }
    }
}
