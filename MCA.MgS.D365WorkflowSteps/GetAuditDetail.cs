using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace MCA.MgS.D365WorkflowSteps
{
    class GetAuditDetail : CodeActivity
    {
        [RequiredArgument]
        [Input("AuditId")]
        public InArgument<string> AuditId { get; set; }

        [Output("Field")]
        public OutArgument<string> Field { get; set; }

        [Output("New Value")]
        public OutArgument<string> NewValue { get; set; }

        [Output("Old Value")]
        public OutArgument<string> OldValue { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var executionContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(executionContext.UserId);
            var auditId = AuditId.Get(context);
            string newValue = string.Empty;
            string oldValue = string.Empty;
            string fieldKey = string.Empty;

            var auditDetailsRequest = new RetrieveAuditDetailsRequest();
            auditDetailsRequest.AuditId = Guid.Parse(auditId);
            var auditDetailsResponse = (RetrieveAuditDetailsResponse)service.Execute(auditDetailsRequest);
            var responseType = (auditDetailsResponse.AuditDetail.GetType()).ToString();

            if (responseType == "Microsoft.Crm.Sdk.Messages.AttributeAuditDetail")
            {
                var auditDetail = (AttributeAuditDetail)auditDetailsResponse.AuditDetail;

                var rawKey = auditDetail.NewValue.Attributes.FirstOrDefault().Key;
                var rawNewValue = auditDetail.NewValue.Attributes.FirstOrDefault().Value;
                var rawOldValue = auditDetail.OldValue.Attributes.FirstOrDefault().Value;

                if (rawNewValue.GetType().ToString() == "Microsoft.Xrm.Sdk.OptionSetValue")
                {
                    rawNewValue = auditDetail.NewValue.FormattedValues.FirstOrDefault().Value;
                    rawOldValue = auditDetail.OldValue.FormattedValues.FirstOrDefault().Value;
                }

                newValue = rawNewValue.ToString();
                oldValue = rawOldValue.ToString();
                fieldKey = rawKey.ToString();

            }
            NewValue.Set(context, newValue);
            OldValue.Set(context, oldValue);
            Field.Set(context, fieldKey);
        }
    }
}
