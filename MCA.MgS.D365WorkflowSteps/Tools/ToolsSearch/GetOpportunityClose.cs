using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCA.MgS.D365WorkflowSteps
{
    public class GetOpportunityClose : CodeActivity
    {
        [RequiredArgument]
        [Input("Opportunity")]
        [ReferenceTarget("opportunity")]
        public InArgument<EntityReference> Opportunity { get; set; }

        [Output("Opportunity Close")]
        [ReferenceTarget("opportunityclose")]
        [Default(null)]
        public OutArgument<EntityReference> OpportunityClose { get; set; }
        
        [Output("Number of Closes")]
        [Default(null)]
        public OutArgument<int> CloseCount { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            //var tracingService = executionContext.GetExtension<ITracingService>();

            var opportunity = Opportunity.Get(executionContext);

            var entity = CrmUtility.GetEntityByAttribute(service, "opportunityclose", "opportunityid", (opportunity.Id).ToString(), new ColumnSet("opportunityid"));
            var totalCount = CrmUtility.GetTotalCountByFetch(service, "opportunityclose", "opportunityid", "and", "opportunityid", (opportunity.Id).ToString(), null, null);

            if (totalCount > 0) OpportunityClose.Set(executionContext, entity.ToEntityReference());
            CloseCount.Set(executionContext, totalCount);
        }
    }
}
