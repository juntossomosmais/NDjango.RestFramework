using System.Collections.Generic;
using Newtonsoft.Json;

namespace NDjango.RestFramework.Errors
{
    public class ValidationErrors
    {
        /// <summary>
        /// Conventional key for object-level validation errors that don't belong to a specific
        /// field. Mirrors DRF's <c>NON_FIELD_ERRORS_KEY</c> (default <c>"non_field_errors"</c>).
        /// Use this from <c>ValidateDestroyAsync</c> and any cross-field <c>ValidateAsync</c>
        /// override that needs to surface a record-level rejection.
        /// </summary>
        public const string NonFieldErrorsKey = "non_field_errors";

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
