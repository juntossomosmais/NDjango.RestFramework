using AspNetCore.RestFramework.Core.Base;
using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Errors
{
    public class ValidationErrors : BaseErrorResponse<string[]>
    {
        private IDictionary<string, string[]> _errors;

        public ValidationErrors(IDictionary<string, string[]> errors)
        {
            _errors = errors;
        }

        public override string Type => "VALIDATION_ERRORS";

        public override IDictionary<string, string[]> Error => _errors;
    }
}
