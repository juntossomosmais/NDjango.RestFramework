using Newtonsoft.Json;

namespace NDjango.RestFramework.Errors
{
    public class UnexpectedError
    {
        public string Type => "UNEXPECTED_ERROR";
        public int StatusCode { get; private set; }
        public UnexpectedErrorDetail Error { get; private set; }

        public UnexpectedError(int statusCode, string msg)
        {
            StatusCode = statusCode;
            Error = new UnexpectedErrorDetail(msg);
        }

        [JsonConstructor]
        private UnexpectedError(int statusCode, UnexpectedErrorDetail error)
        {
            StatusCode = statusCode;
            Error = error;
        }
    }

    public class UnexpectedErrorDetail
    {
        public string Msg { get; private set; }

        [JsonConstructor]
        public UnexpectedErrorDetail(string msg)
        {
            Msg = msg;
        }
    }
}
