using System;
using System.Collections.Generic;
using System.Linq;
using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;

namespace MCA.MgS.D365WorkflowSteps
{
    public class MergeRecords : CodeActivity
    {
        [RequiredArgument]
        [Input("Master Record Id")]
        public InArgument<string> MasterId { get; set; }
        [RequiredArgument]
        [Input("Slave Record Id")]
        public InArgument<string> SlaveId { get; set; }
        [RequiredArgument]
        [Input("Entity Name")]
        public InArgument<string> EntityName { get; set; }
        [RequiredArgument]
        [Input("Enrich Master Record?")]
        public InArgument<bool> EnrichMaster { get; set; }


        protected override void Execute(CodeActivityContext context)
        {
            ITracingService tracingService = context.GetExtension<ITracingService>();

            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var crmService = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            var masterId = MasterId.Get(context);
            var slaveId = SlaveId.Get(context);
            var entityName = EntityName.Get(context);
            var enrichMaster = EnrichMaster.Get(context);

            var masterEntity = new Entity(entityName, Guid.Parse(masterId));
            var slaveEntity = new Entity(entityName, Guid.Parse(slaveId));
            // If user wants to merge data into the master, run an update from EnrichMaster
            if (enrichMaster)
            {
                var updatedEntity = EnrichMasterRecord(masterEntity, slaveEntity);
                crmService.Update(updatedEntity);
            }
            // Merge the two records
            MergeRequest merge = new MergeRequest();

            merge.SubordinateId = slaveEntity.Id;
            merge.Target = masterEntity.ToEntityReference();
            merge.PerformParentingChecks = false;

            var merged = (MergeResponse)crmService.Execute(merge);
        }
        Entity EnrichMasterRecord(Entity duplicateRecord, Entity masterRecord)
        {

            Entity UpdatedEntity = new Entity();
            //intialize
            UpdatedEntity.Id = duplicateRecord.Id;
            UpdatedEntity.LogicalName = duplicateRecord.LogicalName;

            if (UpdatedEntity.Attributes == null)
                UpdatedEntity.Attributes = new AttributeCollection();

            foreach (KeyValuePair<string, object> dupAttribute in duplicateRecord.Attributes) //loop through duplicate records
            {
                // check in master record
                List<KeyValuePair<string, object>> masterAttribute = masterRecord.Attributes.Where(param => (param.Key == dupAttribute.Key)).ToList();

                if (masterAttribute.Count == 0)//not found in master record
                {
                    //add that attibute from dulicate record, but first check it must have value
                    if (dupAttribute.Value != null)
                    {
                        if (dupAttribute.Value.GetType().Equals(typeof(EntityReference))) //if lookup attribute type
                        {
                            EntityReference entityLookup = ((EntityReference)dupAttribute.Value);//get value
                            //make new attrbute and add to subordinate
                            KeyValuePair<string, object> attributeLookup = new KeyValuePair<string, object>(dupAttribute.Key,
                                ((object)(new EntityReference() { Id = entityLookup.Id, LogicalName = entityLookup.LogicalName })));

                            if (!UpdatedEntity.Attributes.Contains(dupAttribute.Key))//now add
                                UpdatedEntity.Attributes.Add(attributeLookup);
                        }
                        else //add non lookup attributes just add
                        {
                            if (!UpdatedEntity.Attributes.Contains(dupAttribute.Key))
                                UpdatedEntity.Attributes.Add(dupAttribute);
                        }
                    }
                }
                else
                {
                    //if found chk it should not null otherwise add the dpulicate attribute value
                    if (masterAttribute[0].Value == null)
                    {
                        if (dupAttribute.Value != null) //it must contain value
                            if (!UpdatedEntity.Attributes.Contains(dupAttribute.Key))
                                UpdatedEntity.Attributes.Add(dupAttribute);
                    }
                }
            }

            return UpdatedEntity;
        }
    }
}
