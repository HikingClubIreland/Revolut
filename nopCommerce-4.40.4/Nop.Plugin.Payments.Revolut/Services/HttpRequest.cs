using System;
using System.Net.Http;

namespace Nop.Plugin.Payments.Revolut.Services
{
    class HttpRequest : HttpRequestMessage
    {
        #region Fields

        string _path;
        HttpMethod _method;
        Type _responseType;
        Object _body;
        string _contentType;
        string _contentEncoding;

        #endregion

        #region Ctor

        public HttpRequest(string path, HttpMethod method)
        {
            _path = path;
            _method = method;
        }

        public HttpRequest(string path, HttpMethod method, Type responseType)
        {
            _path = path;
            _method = method;
            _responseType = responseType;
        }

        #endregion

        #region Methods

        public string Path { get; set; }
        public object Body { get; set; }
        public string ContentType { get; set; }
        public string ContentEncoding { get; set; }
        public Type ResponseType { get; }

        public T Clone<T>(T httpRequestObject)
        {
            T newObject = (T)Activator.CreateInstance(httpRequestObject.GetType());

            foreach (var httpRequestObjectProp in httpRequestObject.GetType().GetProperties())
            {
                httpRequestObjectProp.SetValue(newObject, httpRequestObjectProp.GetValue(httpRequestObject));
            }

            return newObject;
        }

        #endregion
    }
}
