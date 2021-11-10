using System;
using System.Activities;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Xrm.Sdk.Workflow;

namespace MCA.MgS.D365WorkflowSteps
{
    public class D365ACF4CallApi : CodeActivity
    {
        [Input("Service Url")]
        public InArgument<string> ServiceUrl { get; set; }

        [Input("Bearer Token")]
        public InArgument<string> BearerToken { get; set; }

        [Input("Method")]
        public InArgument<string> Method { get; set; }

        [Input("JSON Body")]
        public InArgument<string> JsonBody { get; set; }

        [Output("Response")]
        public OutArgument<string> Response { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var url = ServiceUrl.Get(context);
            var body = JsonBody.Get(context);
            var accesstoken = BearerToken.Get(context);
            var method = Method.Get(context).ToUpper();

            try
            {
                if (method == "GET")
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Headers.Add("Authorization", "Bearer " + accesstoken);
                    request.Accept = "*/*";
                    request.ContentType = "application/json";
                    request.Method = method;
                    var response = String.Empty;
                    using (HttpWebResponse res = (HttpWebResponse)request.GetResponse())
                    {
                        Stream dataStream = res.GetResponseStream();
                        StreamReader reader = new StreamReader(dataStream);
                        response = reader.ReadToEnd();
                        reader.Close();
                        dataStream.Close();
                    }
                    Response.Set(context, response);
                }

                if (method == "POST" || method == "POST" || method == "PATCH")
                {
                    using (var client = new ExtendedWebClient(accesstoken))
                    {
                        var response = client.UploadString(url, method, body ?? string.Empty);
                        Response.Set(context, response);
                    }
                }
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Response.Set(context, ex.Message);
                    ex = ex.InnerException;
                }
            };
        }
        protected class ExtendedWebClient : WebClient
        {
            public ExtendedWebClient(string _accessToken)
            {
                Encoding = Encoding.UTF8;
                Headers.Add(HttpRequestHeader.ContentType, "application/json");
                Headers.Add(HttpRequestHeader.Accept, "*/*");
                Headers.Add(HttpRequestHeader.Authorization, "Bearer" + _accessToken);
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var w = base.GetWebRequest(address);
                w.Timeout = int.MaxValue;
                return w;
            }
        }
    }
}
