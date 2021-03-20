using System;
using Microsoft.Xrm.Sdk;
using System.Activities;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace MCA.MgS.D365WorkflowSteps
{
    public class GetUsersOnPostMention : CodeActivity
    {
        #region Exposed Workflow properties

        [Input("Post")]
        [RequiredArgument]
        [ReferenceTarget("post")]
        public InArgument<EntityReference> Post { get; set; }

        [Output("Poster")]
        public OutArgument<string> Poster { get; set; }
            

        [Output("Mentioned")]
        public OutArgument<string> Response { get; set; }

        #endregion Exposed Workflow properties

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var crmService = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = executionContext.GetExtension<ITracingService>();

            try
            {
                var primaryEntityName = context.PrimaryEntityName;
                var entity = context.PreEntityImages["PreBusinessEntity"];

                if (primaryEntityName != "post")
                {
                    var ent = Post.Get(executionContext);
                    entity = crmService.Retrieve(ent.LogicalName, ent.Id, new ColumnSet(true));
                }
                
                var regardingObjectId = entity.GetEntityReference("regardingobjectid");
                var poster = entity.GetEntityReference("createdby");
                var text = entity.GetString("text");
                var mentions = Utility.SplitStringOnBrackets(text);

                var posterRec = crmService.Retrieve(poster.LogicalName, poster.Id, new ColumnSet("internalemailaddress", "systemuserid"));
                var posterEmail = posterRec["internalemailaddress"].ToString();
                
                string emails = string.Join(",", mentions);

                Response.Set(executionContext, emails);
                Poster.Set(executionContext, posterRec);
                
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(
                    $"An error occurred in the {GetType()} plug-in. {Utility.HandleExceptions(ex)}");
            }
        }
    }
}
;