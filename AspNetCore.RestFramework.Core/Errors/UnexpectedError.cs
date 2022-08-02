using AspNetCore.RestFramework.Core.Base;
using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Errors
{
    public class UnexpectedError : BaseErrorResponse<string>
    {
        private readonly string _message;

        public UnexpectedError(string message)
        {
            _message = message;
        }

        public override string Type => "UNEXPECTED_ERROR";

        public override IDictionary<string, string> Error => new Dictionary<string, string>() {
            { "msg", _message }
        };
    }
}
