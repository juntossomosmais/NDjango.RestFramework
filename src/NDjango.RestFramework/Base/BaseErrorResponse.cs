using System.Collections.Generic;

namespace NDjango.RestFramework.Base
{
    public abstract class BaseErrorResponse<TError>
    {
        public abstract string Type { get; }

        public abstract IDictionary<string, TError> Error { get; set; }
    }
}
