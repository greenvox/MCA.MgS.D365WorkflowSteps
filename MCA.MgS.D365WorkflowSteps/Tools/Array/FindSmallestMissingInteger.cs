using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using System.Activities;
using System.Linq;
using System;

namespace MCA.MgS.D365WorkflowSteps
{
    public class FindSmallestMissingInteger : CodeActivity
    {
        [RequiredArgument]
        [Input("Comma Separated Integers")]
        public InArgument<string> InputIntegers { get; set; }

        [Output("Smallest Missing Integer")]
        public OutArgument<int> MissingInteger { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            string input = InputIntegers.Get(context);

            if (string.IsNullOrWhiteSpace(input))
            {
                throw new InvalidWorkflowException("Input string is empty.");
            }

            try
            {
                // Parse and clean the input
                var numbers = input
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out int n) && n > 0)
                    .Select(int.Parse)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                if (numbers.Count == 0)
                {
                    throw new InvalidWorkflowException("No valid positive integers found.");
                }

                int min = numbers.First();
                int max = numbers.Last();

                // Find smallest missing number in the range [min, max]
                int missing = Enumerable.Range(min, max - min + 1)
                                        .FirstOrDefault(n => !numbers.Contains(n));

                // If none missing in range, return max + 1
                if (missing == 0)
                {
                    missing = max + 1;
                }

                MissingInteger.Set(context, missing);
            }
            catch (Exception ex)
            {
                throw new InvalidWorkflowException("Error processing input: " + ex.Message);
            }
        }
    }
}
