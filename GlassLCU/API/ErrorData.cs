using Newtonsoft.Json;

namespace GlassLCU.API
{
    public class ErrorData
    {
        public string ErrorCode { get; }
        public int HttpStatus { get; }
        public string Message { get; }

        [JsonConstructor]
        internal ErrorData(string errorCode, int httpStatus, string message)
        {
            this.ErrorCode = errorCode;
            this.HttpStatus = httpStatus;
            this.Message = message;
        }
    }
}
