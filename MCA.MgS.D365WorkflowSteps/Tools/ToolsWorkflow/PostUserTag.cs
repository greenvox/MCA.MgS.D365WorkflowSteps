using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using System.Activities;

namespace MCA.MgS.D365WorkflowSteps
{
    public class PostUserTag : CodeActivity
    {
        [RequiredArgument]
        [Input("Post Record")]
        [ReferenceTarget("post")]
        public InArgument<EntityReference> PostRecord { get; set; }
        [RequiredArgument]
        [Input("Client Url")]
        [Default("https://company.crm.dynamics.com/main.aspx")]
        public InArgument<string> ClientUrl { get; set; }
        [Output("Poster")]
        public OutArgument<string> Poster { get; set; }
        [Output("Mentioned")]
        public OutArgument<string> Mentioned { get; set; }
        [Output("Post Body")]
        public OutArgument<string> PostBody { get; set; }
        [Output("Record Url")]
        public OutArgument<string> RecordUrl { get; set; }
        [Output("Entity Name")]
        public OutArgument<string> EntityName { get; set; }
        [Output("Has Mentions")]
        public OutArgument<bool> Status { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            ITracingService tracingService = context.GetExtension<ITracingService>();

            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var Service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            var postrecord = PostRecord.Get(context);
            var clienturl = ClientUrl.Get(context);

            var entity = Service.Retrieve(postrecord.LogicalName, postrecord.Id, new ColumnSet(true));
            //get body of text
            var text = (string)entity["text"];

            //get regarding object
            var regarding = (EntityReference)entity["regardingobjectid"];

            var creator = ((EntityReference)entity["createdby"]).Name;

            // 1. Check if Post type is User Post.
            if (entity.FormattedValues["source"] == "Manual Post")
            {
                if (text.Contains("@"))
                {

                    // 3. String manipulation and try to extract the GUID. Check if valid GUID.
                    var mention = text.Split('[', ']').Where((item, index) => index % 2 != 0).ToList();

                    // 4. Loop through all mentions
                    foreach (var r in mention)
                    {
                        // 5. If valid GUID, check if such an user exists.
                        var nameid = r.Split(',', ',')[1];
                        var name = r.Split('"', '"')[1];

                        // 6. If user exists, create an email by calling a child workflow or an action
                        var user = Service.Retrieve("systemuser", Guid.Parse(nameid), new ColumnSet(true));
                        var emailaddress = user["internalemailaddress"];

                        // 7. Data for e-mail body
                        var sender = (string)entity.FormattedValues["createdby"];
                        var body = (string)text.Replace(r, name);
                        // var entityname = (string)entity.LogicalName;
                        var recordname = ((EntityReference)entity["regardingobjectid"]).Name;
                        var recordurl = clienturl + "?newWindow=true&pagetype=entityrecord&etn=" + regarding.LogicalName + "&id=" + regarding.Id + "";

                        Poster.Set(context, sender);
                        PostBody.Set(context, body);
                        Mentioned.Set(context, emailaddress);
                        RecordUrl.Set(context, recordurl);
                        EntityName.Set(context, recordname);
                        Status.Set(context, true);

                    }
                }
                else
                {
                    Status.Set(context, false);
                }
            }
        }
    }
}
