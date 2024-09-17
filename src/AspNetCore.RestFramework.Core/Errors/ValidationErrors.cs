using AspNetCore.RestFramework.Core.Base;
using System.Collections.Generic;

namespace AspNetCore.RestFramework.Core.Errors
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
