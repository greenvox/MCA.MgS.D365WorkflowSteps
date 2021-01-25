using System;
using System.Linq;
using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.Text;
using System.IO;

namespace SKWorkflowActivities
{
    public class GetFileParts : CodeActivity
    {
        [RequiredArgument]
        [Input("File Path or Name")]
        public InOutArgument<string> FilePath { get; set; }

        [Output("File Name")]
        public OutArgument<string> FileName { get; set; }

        [Output("File Name without Extension")]
        public OutArgument<string> FileNameWithoutExtension { get; set; }

        [Output("File Extension")]
        public OutArgument<string> FileExtension { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            var filePath = FilePath.Get(executionContext);
            var fileName = Path.GetFileName(filePath);
            var fileExtension = Path.GetExtension(filePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            FileNameWithoutExtension.Set(executionContext, fileNameWithoutExtension);
            FileName.Set(executionContext, fileName);
            FileExtension.Set(executionContext, fileExtension);
        }
    }
}


