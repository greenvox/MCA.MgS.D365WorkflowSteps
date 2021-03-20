using System;
using System.Linq;
using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.Text;
using System.IO;

namespace MCA.MgS.D365WorkflowSteps
{
    public class ConvertBase64ToUTF8 : CodeActivity
    {
        [RequiredArgument]
        [Input("Base 64 String")]
        public InArgument<string> Base64String { get; set; }

        [Output("UTF-8 Decoded String")]
        public OutArgument<string> Response { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            var base64String = Base64String.Get(executionContext);
            byte[] data = Convert.FromBase64String(base64String);
            string decodedString = Encoding.UTF8.GetString(data);

            Response.Set(executionContext, decodedString);

        }
    }
}


