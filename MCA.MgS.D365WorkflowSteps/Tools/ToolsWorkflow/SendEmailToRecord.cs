﻿using System;
using Microsoft.Xrm.Sdk;
using System.Activities;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace MCA.MgS.D365WorkflowSteps
{
    public class SendEmailToRecord : CodeActivity
    {
        #region Exposed Workflow properties
        [Input("Sender")]
        [RequiredArgument]
        public InArgument<string> Sender
        {
            get;
            set;
        }
        [Input("Recipients (Comma Seperated)")]
        [RequiredArgument]
        public InArgument<string> Recipients
        {
            get;
            set;
        }
        [Input("Recipient Type (Entity)")]
        [RequiredArgument]
        [Default("contact")]
        public InArgument<string> RecipientType
        {
            get;
            set;
        }

        [Input("Email Schema Name")]
        [RequiredArgument]
        [Default("emailaddress1")]
        public InArgument<string> SchemaName
        {
            get;
            set;
        }

        [Input("Subject")]
        [RequiredArgument]
        public InArgument<string> Subject
        {
            get;
            set;
        }
        [Input("Message Body (HTML or Text)")]
        [RequiredArgument]
        public InArgument<string> Message
        {
            get;
            set;
        }
        [Input("Regarding URL")]
        [RequiredArgument]
        public InArgument<string> Regarding
        {
            get;
            set;
        }

        #endregion Exposed Workflow properties

        protected override void Execute(CodeActivityContext executionContext)
        {
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var crmService = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = executionContext.GetExtension<ITracingService>();

            try
            {
                var subject = Subject.Get(executionContext);
                var message = Message.Get(executionContext);
                var recipients = Recipients.Get(executionContext).Split(new char[] { ',' }).ToList();
                var regarding = CrmUtility.GetRecordID(Regarding.Get(executionContext));
                var sender = Sender.Get(executionContext);
                var htmlTemplate = $"" + message;
                var recipientType = RecipientType.Get(executionContext);
                var schemaName = SchemaName.Get(executionContext);


                foreach (var user in recipients)
                {
                    var fromActivityParty = new Entity("activityparty");
                    var toActivityParty = new Entity("activityparty");

                    fromActivityParty["partyid"] = CrmUtility.GetCrmUser(crmService, sender);
                    toActivityParty["partyid"] = CrmUtility.GetEntityByAttribute(crmService, recipientType, schemaName, user, new ColumnSet(true)).ToEntityReference();

                    var email = new Entity("email")
                    {
                        ["from"] = new[] { fromActivityParty },
                        ["to"] = new[] { toActivityParty },
                        ["regardingobjectid"] = null,
                        ["subject"] = $"" + subject,
                        ["description"] = htmlTemplate,
                        ["directioncode"] = true
                    };

                    var emailId = crmService.Create(email);

                    var sendEmailRequest = new SendEmailRequest
                    {
                        EmailId = emailId,
                        TrackingToken = string.Empty,
                        IssueSend = true
                    };

                    var sendEmailResponse = (SendEmailResponse)crmService.Execute(sendEmailRequest);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(
                    $"An error occurred in the {GetType()} plug-in. {Utility.HandleExceptions(ex)}");
            }
        }
    }
}
