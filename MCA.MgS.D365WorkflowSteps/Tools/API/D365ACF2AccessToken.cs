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
    public class D365ACF2AccessToken : CodeActivity
    {
        [Input("Grant Type")]
        [Default("client_credentials")]
        public InArgument<string> GrantType { get; set; }

        [Input("Access Token")]
        public InArgument<string> AccessToken { get; set; }

        [Input("Client Id (Azure AD)")]
        public InArgument<string> ClientId { get; set; }

        [Input("Client Secret (Azure AD)")]
        public InArgument<string> ClientSecret { get; set; }

        [Input("Resource")]
        [Default("https://contoso-prd.operations.dynamics.com")]
        public InArgument<string> Resource { get; set; }

        [Input("Tenant Id")]
        public InArgument<string> TenantId { get; set; }

        [Output("Response")]
        public OutArgument<string> Response { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var granttype = GrantType.Get(context);
            var accesstoken = AccessToken.Get(context);
            var clientid = ClientId.Get(context);
            var clientsecret = ClientSecret.Get(context);
            var resource = Resource.Get(context).Replace('"', '\"');
            var tenantid = TenantId.Get(context);
            var url = "https://login.microsoftonline.com/" + tenantid + "/oauth2/token";
            var method = "GET";
            var body = "grant_type=" + granttype +
                "&client_id=" + clientid +
                "&client_secret=" + clientsecret +
                "&resource=" + resource;
                
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
