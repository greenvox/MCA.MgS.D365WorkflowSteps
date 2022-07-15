using System;
using System.Linq;
using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MCA.MgS.D365WorkflowSteps
{
    public class GetDateParts : CodeActivity
    {
        [RequiredArgument]
        [Input("Date")]
        public InArgument<DateTime> InputDate { get; set; }
        [Output("Year")]
        public OutArgument<int> OutputYear { get; set; }
        [Output("Month")]
        public OutArgument<int> OutputMonth { get; set; }
        [Output("Day")]
        public OutArgument<int> OutputDay { get; set; }
        [Output("Hour")]
        public OutArgument<int> OutputHour { get; set; }
        [Output("Minute")]
        public OutArgument<int> OutputMinute { get; set; }
        [Output("Second")]
        public OutArgument<int> OutputSecond { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            ITracingService tracingService = context.GetExtension<ITracingService>();

            var inputDate = InputDate.Get(context);

            OutputYear.Set(context, inputDate.Year);
            OutputMonth.Set(context, inputDate.Month);
            OutputDay.Set(context, inputDate.Day);
            OutputHour.Set(context, inputDate.Hour);
            OutputMinute.Set(context, inputDate.Minute);
            OutputSecond.Set(context, inputDate.Second);
        }

        
    }
}
