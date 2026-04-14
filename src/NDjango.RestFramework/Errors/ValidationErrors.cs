using System.Collections.Generic;
using Newtonsoft.Json;

namespace NDjango.RestFramework.Errors
{
    public class ValidationErrors
    {
        public string Type => "VALIDATION_ERRORS";
        public int StatusCode => 400;
        public IDictionary<string, string[]> Error { get; private set; }

        [JsonConstructor]
        public ValidationErrors(IDictionary<string, string[]> error)
        {
            Error = error;
        }
    }
}
