using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Base
{
    public abstract class BaseErrorResponse<TError>
    {
        public abstract string Type { get; }

        public abstract IDictionary<string, TError> Error { get; set; }
    }
}
