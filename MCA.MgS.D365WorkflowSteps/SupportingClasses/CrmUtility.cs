using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace MCA.MgS.D365WorkflowSteps
{
    /// <summary>
    ///     (Credit: Mitch Milam at XrmCoaches.com)
    ///     Common functions to support code activities
    /// </summary>
    public class CrmUtility
    {
        public static EntityReference GetCrmAdminUser(IOrganizationService service)
        {
            var query = new QueryByAttribute(SystemUser.EntityLogicalName);

            query.AddAttributeValue("fullname", "CRM Admin");

            return service.RetrieveMultiple(query).Entities.FirstOrDefault()?.ToEntityReference();
        }

        public static EntityReference CreateEntityReferenceFromString(string value)
        {
            // 0  1                                     2                                    
            // 8, DF108675-DF2B-E911-A964-000D3A3B535A,\"Sarfraz Khan\"]

            const int systemUserType = 8;
            const string entityTypeName = SystemUser.EntityLogicalName;

            var fields = value.Split(',', '[');

            var convertible = int.TryParse(fields[0], out var entityType);

            if (!convertible)
            {
                return null;
            }

            var entityId = new Guid(fields[1]);

            return entityType != systemUserType ? null : new EntityReference(entityTypeName, entityId);
        }

        public static Entity GetEntityByAttribute(IOrganizationService service, string entityName, string fieldName, string fieldValue, ColumnSet columnSet)
        {
            var query = new QueryByAttribute(entityName)
            {
                ColumnSet = columnSet
            };

            query.AddAttributeValue(fieldName, fieldValue);

            return service.RetrieveMultiple(query).Entities.FirstOrDefault();
        }

        public static string GetStringByValueUsingFetch(IOrganizationService service, string entityName, string attributeName, string filter, string fieldName, string fieldValue, string fetchFilters)
        {
            var fetchXml = $@"
                    <fetch>
                      <entity name='{entityName}'>
                        <attribute name='{attributeName}' />
                        <filter type='{filter}'>
                          <condition attribute='{fieldName}' operator='eq' value='{fieldValue}'/>
                          {fetchFilters}  
                        </filter>
                      </entity>
                    </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.FirstOrDefault();

            return result == null ? string.Empty : result.GetString(attributeName);
        }
        public static string GetStringByValueUsingFetch(IOrganizationService service, string entityName, string attributeName, string filter, string fieldName, string fieldValue, string fetchFilters, string orderBy)
        {
            var fetchXml = $@"
                    <fetch>
                      <entity name='{entityName}'>
                        <attribute name='{attributeName}' />
                        <filter type='{filter}'>
                          <condition attribute='{fieldName}' operator='eq' value='{fieldValue}'/>
                          {fetchFilters}  
                        </filter>
                        {orderBy}
                      </entity>
                    </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.FirstOrDefault();

            return result == null ? string.Empty : result.GetString(attributeName);
        }

        public static int GetTotalCountByFetch(IOrganizationService service, string entityName, string attributeName, string filter, string fieldName, string fieldValue, string fetchFilters, string orderBy)
        {
            var fetchXml = $@"
                    <fetch>
                      <entity name='{entityName}'>
                        <attribute name='{attributeName}' />
                        <filter type='{filter}'>
                          <condition attribute='{fieldName}' operator='eq' value='{fieldValue}'/>
                          {fetchFilters}  
                        </filter>
                        {orderBy}
                      </entity>
                    </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.Count;
            return result;
        }


        public static Entity GetEntityByUsingFetch(IOrganizationService service, string entityName, string filter, string fetchFilters)
        {
            var fetchXml = $@"
                    <fetch>
                      <entity name='{entityName}'>
                        <all-attributes/>
                        <filter type='{filter}'>
                          {fetchFilters}  
                        </filter>
                      </entity>
                    </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.FirstOrDefault();
            return result;
        }

        public static string GetRecordID(string recordURL)
        {

            if (recordURL == null || recordURL == "")
            {
                return "";
            }
            string[] urlParts = recordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string objectTypeCode = urlParams[0].Replace("etc=", "");
            //  entityName =  sGetEntityNameFromCode(objectTypeCode, service);
            string objectId = urlParams[1].Replace("id=", "");
            return objectId;
        }

        public static Entity GetEntityByUsingFetch(IOrganizationService service, string entityName, string filter, string fetchFilters, string orderBy)
        {
            var fetchXml = $@"
                    <fetch>
                      <entity name='{entityName}'>
                        <all-attributes/>
                        <filter type='{filter}'>
                          {fetchFilters}  
                        </filter>
                        {orderBy}
                      </entity>
                    </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.FirstOrDefault();
            return result;
        }

        /// <summary>
        ///This method will gives you the optionset text based on value 
        /// </summary>
        /// <param name="entityName"> Entity Name of otionset field </param>
        /// <param name="fieldName">Optionset field Name</param>
        /// <param name="optionSetValue"> Option Set Value</param>
        /// <param name="service">Organization Service</param>
        /// <returns></returns>

        public string GetOptionSetTextUsingValue(string entityName, string fieldName, int optionSetValue, IOrganizationService service)
        {

            var attReq = new RetrieveAttributeRequest();
            attReq.EntityLogicalName = entityName;
            attReq.LogicalName = fieldName;
            attReq.RetrieveAsIfPublished = true;
            var attResponse = (RetrieveAttributeResponse)service.Execute(attReq);
            var attMetadata = (EnumAttributeMetadata)attResponse.AttributeMetadata;
            return attMetadata.OptionSet.Options.Where(x => x.Value == optionSetValue).FirstOrDefault().Label.UserLocalizedLabel.Label;

        }
    }
}
