using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace Nop.Plugin.Payments.Revolut.Services
{
    class HttpException : IOException
    {
        HttpStatusCode _statusCode;
        HttpHeaders _httpHeaders;
        string _message;
        public HttpException(HttpStatusCode statusCode, HttpHeaders headers, string message)
        {
            _statusCode = statusCode;
            _httpHeaders = headers;
            _message = message;
        }

        public HttpStatusCode StatusCode { get; }
        public HttpHeaders Headers { get; }
    }
}
