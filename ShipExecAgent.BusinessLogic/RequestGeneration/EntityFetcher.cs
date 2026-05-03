using PSI.Sox.Data;
using PSI.Sox.Wcf;
using Microsoft.Extensions.Logging;
using ShipExecAgent.BusinessLogic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ShipExecAgent.BusinessLogic.RequestGeneration
{
    /// <summary>
    /// Lightweight base class for fetching a collection of entities from the Administration API.
    /// Focused solely on the GetAll operation; use RequestGenerationBase for full CRUD.
    /// </summary>
    public abstract class EntityFetcher<TRequest, TResponse>
        where TRequest : RequestBase, new()
        where TResponse : new()
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly ILogger _logger = LoggerProvider.CreateLogger<EntityFetcher<TRequest, TResponse>>();

        public string AdminUrl { get; set; }
        public Guid CompanyId { get; }
        public string Jwt { get; set; }
        public string Endpoint { get; }

        protected EntityFetcher(string adminUrl, Guid companyId, string jwt, string endpoint)
        {
            AdminUrl = adminUrl;
            CompanyId = companyId;
            Jwt = jwt;
            Endpoint = endpoint;
        }

        /// <summary>
        /// Override to set CompanyId and any other required fields on the request before it is sent.
        /// </summary>
        public virtual TRequest ConfigureRequest(TRequest request)
        {
            return request;
        }

        protected SearchCriteria GetEmptySearchCriteria()
        {
            return new SearchCriteria
            {
                WhereClauses = new List<WhereClause>(),
                OrderByClauses = new List<OrderByClause>()
            };
        }

        /// <summary>
        /// Posts the configured request to the endpoint and returns the deserialized response.
        /// </summary>
        public TResponse Fetch()
        {
            _logger.LogTrace(">> Fetch | Endpoint={Endpoint} RequestType={RequestType}",
                Endpoint, typeof(TRequest).Name);
            TRequest request = ConfigureRequest(new TRequest());

            var endpoint = AdminUrl + Endpoint;

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Jwt);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = _httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var result = JsonHelper.Deserialize<TResponse>(content);
                _logger.LogTrace("<< Fetch | Endpoint={Endpoint} ResponseType={ResponseType}",
                    Endpoint, typeof(TResponse).Name);
                return result;
            }
        }
    }

    /// <summary>
    /// Base class for fetching a collection of site-scoped entities from the Administration API.
    /// </summary>
    public abstract class SiteEntityFetcher<TRequest, TResponse>
        : EntityFetcher<TRequest, TResponse>
        where TRequest : RequestBase, new()
        where TResponse : new()
    {
        public Guid SiteId { get; }

        protected SiteEntityFetcher(string adminUrl, Guid companyId, Guid siteId, string jwt, string endpoint)
            : base(adminUrl, companyId, jwt, endpoint)
        {
            SiteId = siteId;
        }
    }
}
