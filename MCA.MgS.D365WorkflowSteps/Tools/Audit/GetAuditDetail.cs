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
using Newtonsoft.Json;

namespace MCA.MgS.D365WorkflowSteps
{
    public class GetAuditDetail : CodeActivity
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

        [Output("Audit Type")]
        public OutArgument<string> AuditType { get; set; }

        [Output("New Role Privileges")]
        public OutArgument<string> NewRolePrivileges { get; set; }

        [Output("Old Role Privileges")]
        public OutArgument<string> OldRolePrivileges { get; set; }

        [Output("Relationship Name")]
        public OutArgument<string> RelationshipName { get; set; }

        [Output("Target Records")]
        public OutArgument<string> TargetRecords { get; set; }

        [Output("New Privileges")]
        public OutArgument<string> NewPrivileges { get; set; }

        [Output("Old Privileges")]
        public OutArgument<string> OldPrivileges { get; set; }

        [Output("Principal Name")]
        public OutArgument<string> PrincipalName { get; set; }

        [Output("Access Time")]
        public OutArgument<string> AccessTime { get; set; }

        [Output("Interval")]
        public OutArgument<string> Interval { get; set; }

        [Output("Serialized Response")]
        public OutArgument<string> SerializedResponse { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var executionContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(executionContext.UserId);
            var auditId = AuditId.Get(context);

            string newValue = string.Empty;
            string oldValue = string.Empty;
            string fieldKey = string.Empty;
            string newRolePrivileges = string.Empty;
            string oldRolePrivileges = string.Empty;
            string relationshipName = string.Empty;
            string targetRecords = string.Empty;
            string newPrivileges = string.Empty;
            string oldPrivileges = string.Empty;
            string principalName = string.Empty;
            string accessTime = string.Empty;
            string interval = string.Empty;
            string auditType = string.Empty;
            string status = string.Empty;

            var auditDetailsRequest = new RetrieveAuditDetailsRequest();
            auditDetailsRequest.AuditId = Guid.Parse(auditId);
            var auditDetailsResponse = (RetrieveAuditDetailsResponse)service.Execute(auditDetailsRequest);
            var responseType = (auditDetailsResponse.AuditDetail.GetType()).ToString();

            try { 
                if (responseType == "Microsoft.Crm.Sdk.Messages.AttributeAuditDetail")
                {
                    var auditDetail = (AttributeAuditDetail)auditDetailsResponse.AuditDetail;
                    var rawKey = auditDetail.NewValue.Attributes.Keys.Count().ToString();
                    var newVal = new StringBuilder();
                    var oldVal = new StringBuilder();
                    
                    var nvc = auditDetail.NewValue.FormattedValues;
                    var ovc = auditDetail.OldValue.FormattedValues;
                    var nva = auditDetail.NewValue.Attributes;
                    var ova = auditDetail.OldValue.Attributes;

                    var formattedTypes = new string[] { "DateTime", "OptionSetValue", "Money", "EntityReference", "Empty" };
                    var keys = auditDetail.NewValue.Attributes.Keys;
                    foreach (var key in keys)
                    {
                        string str;
                        object obj;
                        nva.TryGetValue(key, out obj);
                        var nObj = (obj == null) ? obj.ToString() : "Empty";
                        if (formattedTypes.Any(c => nObj.Contains(c)))
                        {
                            nvc.TryGetValue(key, out str);
                            newVal.Append(key + ": " + str);
                        }
                        else
                        {
                            newVal.Append(key + ": " + obj.ToString());
                        }
                        newVal.AppendLine();

                        ova.TryGetValue(key, out obj);
                        var oObj = (obj != null) ? obj.ToString() : "Empty";
                        if (formattedTypes.Any(c => oObj.Contains(c)))
                        {
                            ovc.TryGetValue(key, out str);
                            oldVal.Append(key + ": " + str);
                        }
                        else
                        {
                            oldVal.Append(key + ": " + obj.ToString());
                        }
                        oldVal.AppendLine();
                    }

                    newValue = (newVal != null) ? newVal.ToString() : string.Empty;
                    oldValue = (oldVal != null) ? oldVal.ToString() : string.Empty;
                    fieldKey = rawKey.ToString();
                }
                if (responseType == "Microsoft.Crm.Sdk.Messages.RolePrivilegeAuditDetail")
                {
                    var auditDetail = (RolePrivilegeAuditDetail)auditDetailsResponse.AuditDetail;
                    newRolePrivileges = (auditDetail.NewRolePrivileges.FirstOrDefault().PrivilegeId).ToString();
                    oldRolePrivileges = (auditDetail.OldRolePrivileges.FirstOrDefault().PrivilegeId).ToString();
                }
                if (responseType == "Microsoft.Crm.Sdk.Messages.RelationshipAuditDetail")
                {
                    var auditDetail = (RelationshipAuditDetail)auditDetailsResponse.AuditDetail;
                    relationshipName = auditDetail.RelationshipName.ToString();
                    var lstTargetRecords = auditDetail.TargetRecords.ToList();
                    StringBuilder tr = new StringBuilder();
                    foreach (var targetRecord in lstTargetRecords)
                    {
                        tr.Append(targetRecord.Name);
                    }
                    targetRecords = tr.ToString();
                }
                if (responseType == "Microsoft.Crm.Sdk.Messages.ShareAuditDetail")
                {
                    var auditDetail = (ShareAuditDetail)auditDetailsResponse.AuditDetail;
                    newPrivileges = auditDetail.NewPrivileges.ToString();
                    oldPrivileges = auditDetail.OldPrivileges.ToString();
                    principalName = (auditDetail.Principal.Name).ToString();
                }
                if (responseType == "Microsoft.Crm.Sdk.Messages.UserAccessAuditDetail")
                {
                    var auditDetail = (UserAccessAuditDetail)auditDetailsResponse.AuditDetail;
                    accessTime = auditDetail.AccessTime.ToString();
                    interval = auditDetail.Interval.ToString();
                }
            } catch (Exception ex)
            {
                status = ex.Message;
            }
            auditType = responseType.ToString().Replace("Microsoft.Crm.Sdk.Messages.","");

            var jsonRespObj = new
            {
                newvalue = newValue,
                oldvalue = oldValue,
                field = fieldKey,
                audittype = auditType,
                newprivileges = newPrivileges,
                oldprivileges = oldPrivileges,
                newroleprivileges = newRolePrivileges,
                oldroleprivileges = oldRolePrivileges,
                principalname = principalName,
                targetrecords = targetRecords,
                relationshipname = relationshipName,
                accesstime = accessTime,
                accessinterval = interval,
                status = status
            };
            var serializedResponse = JsonConvert.SerializeObject(jsonRespObj);

            NewValue.Set(context, newValue);
            OldValue.Set(context, oldValue);
            Field.Set(context, fieldKey);
            AuditType.Set(context, auditType);
            NewPrivileges.Set(context, newPrivileges);
            OldPrivileges.Set(context, oldPrivileges);
            PrincipalName.Set(context, principalName);
            OldRolePrivileges.Set(context, oldRolePrivileges);
            NewRolePrivileges.Set(context, newRolePrivileges);
            RelationshipName.Set(context, relationshipName);
            TargetRecords.Set(context, targetRecords);
            AccessTime.Set(context, accessTime);
            Interval.Set(context, interval);
            SerializedResponse.Set(context, serializedResponse);
        }
    }
}
