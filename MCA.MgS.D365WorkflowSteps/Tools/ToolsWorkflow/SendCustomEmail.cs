using System;
using Microsoft.Xrm.Sdk;
using System.Activities;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Collections.Generic;

namespace MCA.MgS.D365WorkflowSteps
{
    public class SendCustomEmail : CodeActivity
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

        [Input("Regarding ID")]
        public InArgument<string> RegardingId
        {
            get;
            set;
        }
        [Input("Regarding Entity")]
        public InArgument<string> RegardingEntity
        {
            get;
            set;
        }
        [Input("Send Email?")]
        [RequiredArgument]
        public InArgument<bool> SendEmail
        {
            get;
            set;
        }

        [Output("EmailID")]
        public OutArgument<string> EmailID
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
                var regardingId = RegardingId.Get(executionContext);
                var regardingEntity = RegardingEntity.Get(executionContext);
                var regarding = new EntityReference();
                var sender = Sender.Get(executionContext);
                var htmlTemplate = $"" + message;
                var sendEmail = SendEmail.Get(executionContext);

                if (regardingId != null && regardingEntity != null) {
                    regarding = new EntityReference(regardingEntity, Guid.Parse(regardingId));
                }

                var parties = new List<Entity>();

                foreach (var user in recipients)
                {
                    var toActivityParty = new Entity("activityparty");
                    var toQuery = new QueryByAttribute("systemuser");
                    toQuery.AddAttributeValue("internalemailaddress", user);
                    var toResults = crmService.RetrieveMultiple(toQuery).Entities;
                    if (toResults.Count > 0)
                    {
                        try {
                            var userRef = toResults.FirstOrDefault().ToEntityReference();
                            toActivityParty["partyid"] = userRef;
                            parties.Add(toActivityParty);
                        } catch (Exception ex)
                        {
                            tracingService.Trace(ex.Message);
                        }
                    }
                }

                foreach (var contact in recipients)
                {
                    var toActivityParty = new Entity("activityparty");
                    var toQuery = new QueryByAttribute("contact");
                    toQuery.AddAttributeValue("emailaddress1", contact);
                    var toResults = crmService.RetrieveMultiple(toQuery).Entities;
                    if (toResults.Count > 0)
                    {
                        try { 
                            var contactRef = toResults.FirstOrDefault().ToEntityReference();
                            toActivityParty["partyid"] = contactRef;
                            parties.Add(toActivityParty);
                        } catch (Exception ex)
                        {
                            tracingService.Trace(ex.Message);
                        }
                    }
                }

                var fromActivityParty = new Entity("activityparty");
                try { 
                fromActivityParty["partyid"] = CrmUtility.GetCrmUser(crmService, sender);
                } catch (Exception ex)
                {
                    tracingService.Trace(ex.Message);
                }

                var email = new Entity("email")
                {
                    ["from"] = new[] { fromActivityParty },
                    ["to"] = parties.ToArray(),
                    ["regardingobjectid"] = null,
                    ["subject"] = $"" + subject,
                    ["description"] = htmlTemplate,
                    ["directioncode"] = true
                };

                if (!string.IsNullOrEmpty(regardingId) && !string.IsNullOrEmpty(regardingId))
                {
                    email["regardingobjectid"] = regarding;
                }

                var emailId = crmService.Create(email);

                if (sendEmail == true)
                {
                    var sendEmailRequest = new SendEmailRequest
                    {
                        EmailId = emailId,
                        TrackingToken = string.Empty,
                        IssueSend = true
                    };
                    var sendEmailResponse = (SendEmailResponse)crmService.Execute(sendEmailRequest);
                }

                EmailID.Set(executionContext, Convert.ToString(emailId));

            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(
                    $"An error occurred in the {GetType()} plug-in. {Utility.HandleExceptions(ex)}");
            }
        }
    }
}
