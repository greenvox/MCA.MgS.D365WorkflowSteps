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
    public class D365ACF1GetToken : CodeActivity
    {
        [Input("Grant Type")]
        [Default("password")]
        public InArgument<string> GrantType { get; set; }

        [Input("Username")]
        public InArgument<string> Username { get; set; }

        [Input("Password")]
        public InArgument<string> Password { get; set; }

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
            var username = Username.Get(context);
            var password = Password.Get(context);
            var clientid = ClientId.Get(context);
            var clientsecret = ClientSecret.Get(context);
            var resource = Resource.Get(context).Replace('"', '\"');
            var tenantid = TenantId.Get(context);
            var url = "https://login.microsoftonline.com/" + tenantid + "/oauth2/token";
            var method = "POST";
            var body = "grant_type=" + granttype +
                "&username=" + username +
                "&password=" + password +
                "&client_id=" + clientid +
                "&client_secret=" + clientsecret +
                "&resource=" + resource;
                
            try
            {
                using (var client = new ExtendedWebClient())
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
            public ExtendedWebClient()
            {
                Encoding = Encoding.UTF8;
                Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
                Headers.Add(HttpRequestHeader.Accept, "*/*");
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
