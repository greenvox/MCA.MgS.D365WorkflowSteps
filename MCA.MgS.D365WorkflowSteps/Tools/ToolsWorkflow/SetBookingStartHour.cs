using System;
using System.Linq;
using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCA.MgS.D365WorkflowSteps
{
    public class SetBookingStartHour : CodeActivity
    {
        [RequiredArgument]
        [Input("Booking")]
        [ReferenceTarget("bookableresourcebooking")]
        public InArgument<EntityReference> Booking { get; set; }
        [Input("Day Start Hour")]
        public InArgument<int> DayStartHour { get; set; }
        [Input("Add Resource Timezone Bias?")]
        public InArgument<bool> AddTimeZoneBias { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            ITracingService tracingService = context.GetExtension<ITracingService>();

            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var crmService = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            var startHour = DayStartHour.Get(context);
            if (startHour == 0)
            {
                startHour = 8;
            }
            var addTimeZoneBias = AddTimeZoneBias.Get(context);
            var booking = crmService.Retrieve("bookableresourcebooking", Booking.Get(context).Id, new ColumnSet("starttime", "endtime", "duration", "resource"));
            tracingService.Trace("Original Start Time: " + booking["starttime"].ToString());
            tracingService.Trace("Original End Time: " + booking["endtime"].ToString());

            System.DateTime oStartTime = (DateTime)booking["starttime"];
            System.DateTime oEndTime = (DateTime)booking["endtime"];
            int duration = (int)booking["duration"];

            var resource = crmService.Retrieve("bookableresource", ((EntityReference)booking["resource"]).Id, new ColumnSet("timezone", "bookableresourceid"));

                int startBias = startHour + 0;
                if (addTimeZoneBias == true)
                {
                    var timeZoneCode = (int)resource["timezone"];

                    var timeZoneQuery = new QueryExpression("timezonedefinition");
                    timeZoneQuery.TopCount = 1;
                    timeZoneQuery.ColumnSet.AddColumns("standardname", "bias", "timezonecode");
                    timeZoneQuery.Criteria.AddCondition("timezonecode", ConditionOperator.Equal, timeZoneCode);

                    var timeZone = crmService.RetrieveMultiple(timeZoneQuery).Entities.FirstOrDefault();
                    startBias = startHour + ((int)timeZone["bias"] / 60);
                }
            System.DateTime nStartTime = new DateTime(oStartTime.Year, oStartTime.Month, oStartTime.Day, startBias, 0, 0, 0);
            System.DateTime nEndTime = nStartTime.AddMinutes(duration);


            tracingService.Trace("Original Start Time: " + booking["starttime"].ToString());
            tracingService.Trace("Original End Time: " + booking["endtime"].ToString());

            Entity bookingUpdate = new Entity(booking.LogicalName, booking.Id);
            bookingUpdate["starttime"] = nStartTime;
            bookingUpdate["endtime"] = nEndTime;
            crmService.Update(bookingUpdate);

        }

        
    }
}
