using PSI.Sox.Data;
using PSI.Sox.Wcf;
using ShipExecNavigator.BusinessLogic.CompanyBuilder;
using ShipExecNavigator.BusinessLogic.EntityComparison;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ShipExecNavigator.BusinessLogic.RequestGeneration
{
    public class RequestGenerationBase<GetAllResponse, GetAllRequest, GetRequest, GetResponse, AddRequest, AddResponse, UpdateRequest, UpdateResponse, RemoveRequest, RemoveResponse, EntityModel>
        where GetAllResponse : new()
        where GetAllRequest : RequestBase, new()
        where GetRequest : RequestBase, new()
        where GetResponse : ResponseBase, new()
        where AddRequest : RequestBase, new()
        where AddResponse : ResponseBase, new()
        where UpdateRequest : RequestBase, new()
        where UpdateResponse : ResponseBase, new()
        where RemoveRequest : RequestBase, new()
        where RemoveResponse : ResponseBase, new()
        where EntityModel : new()
    {
        public string JWT { get; set; }

        public Guid CompanyGuid { get; set; }

        public string AdminUrl { get; set; }

        public string GetAllEndpoint { get; set; }

        public string GetEndpoint { get; set; }

        public string AddEndpoint { get; set; }

        public string RemoveEndpoint { get; set; }

        public string UpdateEndpoint { get; set; }

        public string EntityName { get; set; }


        public RequestGenerationBase(string adminUrl, 
            Guid companyGuid, 
            string getAllEndpoint,
            string getEndpoint,
            string addEndpoint,
            string updateEndpoint,
            string removeEndpoint,
            string entityName,
            string jwt)
        {
            AdminUrl = adminUrl;
            CompanyGuid = companyGuid;
            JWT = jwt;


            GetAllEndpoint = getAllEndpoint;
            GetEndpoint = getEndpoint;
            AddEndpoint = addEndpoint;
            UpdateEndpoint = updateEndpoint;
            RemoveEndpoint = removeEndpoint;
            EntityName = entityName;
        }

        public GetAllResponse Get()
        {
            var request = new GetAllRequest();
            request = ModifyGetAllRequest(request);

            return Get(request);
        }

        public virtual GetAllRequest ModifyGetAllRequest(GetAllRequest request) => request;

        public GetAllResponse Get(GetAllRequest getRequest)
        {
            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AdminUrl + GetAllEndpoint))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
                var json = JsonHelper.Serialize(getRequest);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<GetAllResponse>(resultContent);
            }
        }

        public GetResponse Get(int id)
        {
            GetRequest getRequest = new GetRequest();
            getRequest = ModifyGetRequestWithId(getRequest, id);
            return Get(getRequest);
        }

        public GetResponse Get(GetRequest getRequest)
        {
            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AdminUrl + GetEndpoint))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
                var json = JsonHelper.Serialize(getRequest);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<GetResponse>(resultContent);
            }
        }

        public virtual GetRequest ModifyGetRequestWithId(GetRequest request, int id) => request;

        public AddResponse BaseAdd(AddRequest addRequest)
        {
            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AdminUrl + AddEndpoint))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
                var json = JsonHelper.Serialize(addRequest);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                //Debugger.Break(); // ← breakpoint: before apply-changes Add API call
                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<AddResponse>(resultContent);
            }
        }

        public UpdateResponse BaseUpdate(UpdateRequest updateRequest)
        {
            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AdminUrl + UpdateEndpoint))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
                var json = JsonHelper.Serialize(updateRequest);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                //Debugger.Break(); // ← breakpoint: before apply-changes Update API call
                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<UpdateResponse>(resultContent);
            }
        }

        public RemoveResponse BaseRemove(RemoveRequest removeRequest)
        {
            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, AdminUrl + RemoveEndpoint))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
                var json = JsonHelper.Serialize(removeRequest);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                //Debugger.Break(); // ← breakpoint: before apply-changes Remove API call
                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<RemoveResponse>(resultContent);
            }
        }

        public EntityModel Find(EntityModel entityModel, List<EntityModel> entityModels)
            => entityModels.FirstOrDefault(x => HasSameId(entityModel, x));

        public virtual bool HasSameId(EntityModel current, EntityModel modified) => false;

        public virtual bool ShouldUpdate(EntityModel current, EntityModel modified) => false;

        public List<Variance> GetVariances(List<EntityModel> current, List<EntityModel> modified)
        {
            var result = new List<Variance>();

            foreach (var existingItem in current)
            {
                var matchingItem = Find(existingItem, modified);
                if (matchingItem == null)
                {
                    result.Add(new Variance
                    {
                        IsRemove = true,
                        OriginalObject = existingItem,
                        EntityName = EntityName
                    });
                }
                else if (ShouldUpdate(existingItem, matchingItem))
                {
                    result.Add(new Variance
                    {
                        IsUpdated = true,
                        OriginalObject = existingItem,
                        NewObject = matchingItem,
                        EntityName = EntityName
                    });
                }
            }

            foreach (var newItem in modified)
            {
                if (Find(newItem, current) == null)
                {
                    result.Add(new Variance
                    {
                        IsAdd = true,
                        NewObject = newItem,
                        EntityName = EntityName
                    });
                }
            }

            return result;
        }

        public virtual AddRequest InitializeAddRequest(ref AddRequest addRequest, Variance variance) => addRequest;

        public virtual UpdateRequest InitializeUpdateRequest(ref UpdateRequest updateRequest, Variance variance) => updateRequest;

        public virtual RemoveRequest InitializeRemoveRequest(ref RemoveRequest removeRequest, Variance variance) => removeRequest;

        public List<RequestBaseWithURL> GetScripts(List<Variance> variances, List<EntityModel> itemsWithIds)
        {
            var result = new List<RequestBaseWithURL>();

            foreach (var variance in variances)
            {
                if (variance.IsAdd)
                {
                    var addRequest = new AddRequest();
                    InitializeAddRequest(ref addRequest, variance);
                    result.Add(new RequestBaseWithURL
                    {
                        Request = addRequest,
                        Endpoint = AdminUrl + AddEndpoint,
                        IsAdd = true,
                        EntityName = EntityName,
                        Variance = variance
                    });
                }
                else if (variance.IsRemove)
                {
                    var removeRequest = new RemoveRequest();
                    InitializeRemoveRequest(ref removeRequest, variance);
                    result.Add(new RequestBaseWithURL
                    {
                        Request = removeRequest,
                        Endpoint = AdminUrl + RemoveEndpoint,
                        IsDelete = true,
                        EntityName = EntityName,
                        Variance = variance
                    });
                }
                else if (variance.IsUpdated)
                {
                    var updateRequest = new UpdateRequest();
                    InitializeUpdateRequest(ref updateRequest, variance);
                    result.Add(new RequestBaseWithURL
                    {
                        Request = updateRequest,
                        Endpoint = AdminUrl + UpdateEndpoint,
                        IsUpdated = true,
                        EntityName = EntityName,
                        Variance = variance
                    });
                }
            }

            return result;
        }

        public SearchCriteria GetEmptySearchCriteria()
        {
            return new SearchCriteria
            {
                WhereClauses = new List<WhereClause>(),
                OrderByClauses = new List<OrderByClause>()
            };
        }
    }
}
