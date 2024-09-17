using System.Collections.Generic;
using NDjango.RestFramework.Base;

namespace NDjango.RestFramework.Errors
{
    public class ValidationErrors : BaseErrorResponse<string[]>
    {
        public ValidationErrors(IDictionary<string, string[]> errors)
        {
        }

        public override string Type => "VALIDATION_ERRORS";

        public override IDictionary<string, string[]> Error { get; set; } = new Dictionary<string, string[]>();
    }
}
