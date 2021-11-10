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
    public class AuditLog
    {
        public string AuditId { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
        public string FieldKey { get; set; }
        public string NewRolePrivileges { get; set; }
        public string OldRolePrivileges { get; set; }
        public string RelationshipName { get; set; }
        public string TargetRecords { get; set; }
        public string NewPrivileges { get; set; }
        public string OldPrivileges { get; set; }
        public string PricipalName { get; set; }
        public string AccessTime { get; set; }
        public string AuditType { get; set; }
    }
    public class ExportAuditDetails : CodeActivity
    {
        [RequiredArgument]
        [Input("Audit Range")]
        [Default("Today, Yesterday, Last7Days, LastWeek, LastMonth etc.")]
        public InArgument<string> AuditRange { get; set; }


        [Output("Serialized Response")]
        public OutArgument<string> SerializedResponse { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var executionContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(executionContext.UserId);
            
            var auditRange = AuditRange.Get(context);
            var dateRange = (ConditionOperator)Enum.Parse(typeof(ConditionOperator), auditRange);
            
            var auditRecords = new List<AuditLog>();
            
            var auditQuery = new QueryExpression("audit");
            auditQuery.ColumnSet.AllColumns = true;
            auditQuery.Criteria.AddCondition("createdon", dateRange);

            var audits = service.RetrieveMultiple(auditQuery).Entities;

            for (var i = 0; i < audits.Count; i++)
            {
                var auditId = audits[i].Id;
                string status = string.Empty;

                var rec = new AuditLog();
                rec.AuditId = auditId.ToString();

                var auditDetailsRequest = new RetrieveAuditDetailsRequest();
                auditDetailsRequest.AuditId = auditId;
                var auditDetailsResponse = (RetrieveAuditDetailsResponse)service.Execute(auditDetailsRequest);
                var responseType = (auditDetailsResponse.AuditDetail.GetType()).ToString();

                try
                {
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

                        rec.NewValue = (newVal != null) ? newVal.ToString() : string.Empty;
                        rec.OldValue = (oldVal != null) ? oldVal.ToString() : string.Empty;
                        rec.FieldKey = rawKey.ToString();


                    }
                    if (responseType == "Microsoft.Crm.Sdk.Messages.RolePrivilegeAuditDetail")
                    {
                        var auditDetail = (RolePrivilegeAuditDetail)auditDetailsResponse.AuditDetail;
                        rec.NewRolePrivileges = (auditDetail.NewRolePrivileges.FirstOrDefault().PrivilegeId).ToString();
                        rec.OldRolePrivileges = (auditDetail.OldRolePrivileges.FirstOrDefault().PrivilegeId).ToString();
                    }
                    if (responseType == "Microsoft.Crm.Sdk.Messages.RelationshipAuditDetail")
                    {
                        var auditDetail = (RelationshipAuditDetail)auditDetailsResponse.AuditDetail;
                        rec.RelationshipName = auditDetail.RelationshipName.ToString();
                        var lstTargetRecords = auditDetail.TargetRecords.ToList();
                        StringBuilder tr = new StringBuilder();
                        foreach (var targetRecord in lstTargetRecords)
                        {
                            tr.Append(targetRecord.Name);
                        }
                        rec.TargetRecords = tr.ToString();
                    }
                    if (responseType == "Microsoft.Crm.Sdk.Messages.ShareAuditDetail")
                    {
                        var auditDetail = (ShareAuditDetail)auditDetailsResponse.AuditDetail;
                        rec.NewPrivileges = auditDetail.NewPrivileges.ToString();
                        rec.OldPrivileges = auditDetail.OldPrivileges.ToString();
                        rec.PricipalName = (auditDetail.Principal.Name).ToString();
                    }
                    if (responseType == "Microsoft.Crm.Sdk.Messages.UserAccessAuditDetail")
                    {
                        var auditDetail = (UserAccessAuditDetail)auditDetailsResponse.AuditDetail;
                        rec.AccessTime = auditDetail.AccessTime.ToString();
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                }
                rec.AuditType = responseType.ToString().Replace("Microsoft.Crm.Sdk.Messages.", "");
                

                auditRecords.Add(rec);
            }
            var serializedResponse = JsonConvert.SerializeObject(auditRecords.ToArray());

            SerializedResponse.Set(context, serializedResponse);
        }
            



           
    }
}
