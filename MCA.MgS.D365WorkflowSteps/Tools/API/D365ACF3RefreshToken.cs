using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Xrm.Sdk.Workflow;
using Newtonsoft.Json;

namespace MCA.MgS.D365WorkflowSteps
{
    public class D365ACF3RefreshToken : CodeActivity
    {
        [Input("Grant Type")]
        [Default("refresh_token")]
        public InArgument<string> GrantType { get; set; }

        [Input("Refresh Token")]
        public InArgument<string> RefreshToken { get; set; }

        [Input("Access Token")]
        public InArgument<string> AccessToken { get; set; }

        [Input("Client Id (Azure AD)")]
        public InArgument<string> ClientId { get; set; }

        [Input("Client Secret (Azure AD)")]
        public InArgument<string> ClientSecret { get; set; }

        [Input("Tenant Id")]
        public InArgument<string> TenantId { get; set; }

        [Output("Response")]
        public OutArgument<string> Response { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var granttype = GrantType.Get(context);
            var accesstoken = AccessToken.Get(context);
            var refreshtoken = RefreshToken.Get(context);
            var clientid = ClientId.Get(context);
            var clientsecret = ClientSecret.Get(context);
            var tenantid = TenantId.Get(context);
            var url = "https://login.microsoftonline.com/" + tenantid + "/oauth2/v2.0/token";
            var method = "GET";
            var body = "grant_type=" + granttype +
                "&client_id=" + clientid +
                "&client_secret=" + clientsecret +
                "&refresh_token=" + refreshtoken;
                
            try
            {
                using (var client = new ExtendedWebClient(accesstoken))
                {
                    var data = client.UploadString(url, method, body ?? string.Empty);
                    Response.Set(context, data);
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
                Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
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
