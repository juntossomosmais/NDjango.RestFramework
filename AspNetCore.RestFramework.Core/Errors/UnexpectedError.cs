using AspNetCore.RestFramework.Core.Base;
using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Errors
{
    public class UnexpectedError : BaseErrorResponse<string>
    {
        private IDictionary<string, string> _error;

        public UnexpectedError()
            : this(BaseMessages.ERROR_MESSAGE)
        { }

        public UnexpectedError(string message)
        {
            _error = new Dictionary<string, string>() {
                { "msg", message }
            };
        }

        public override string Type => "UNEXPECTED_ERROR";

        public override IDictionary<string, string> Error
        {
            get { return _error; }
            set { _error = value; }
        }
    }
}
