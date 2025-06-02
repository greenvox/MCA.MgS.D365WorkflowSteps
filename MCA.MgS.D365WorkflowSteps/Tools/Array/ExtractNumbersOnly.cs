using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace MCA.MgS.D365WorkflowSteps
{
    public class ExtractNumbersOnly : CodeActivity
    {
        [RequiredArgument]
        [Input("Input String")]
        public InArgument<string> InputString { get; set; }

        [Output("Numbers Only String")]
        public OutArgument<string> OutputString { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            var input = InputString.Get(executionContext) ?? string.Empty;
            var numbersOnly = Regex.Replace(input, "[^0-9]", "");
            OutputString.Set(executionContext, numbersOnly);
        }
    }
}
