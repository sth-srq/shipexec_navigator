using PSI.Sox.Data;
using PSI.Sox.Wcf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ShipExecNavigator.BusinessLogic.RequestGeneration
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
            TRequest request = ConfigureRequest(new TRequest());

            var endpoint = AdminUrl + Endpoint;
            //var tempJWT = "yJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGVuYyNyc2Etb2FlcCIsImVuYyI6IkExMjhDQkMtSFMyNTYiLCJraWQiOiIzQjJBRkM2MDdEQjlEM0NBQTFBOTJGMTc2QUVDNTM4Mzg3MTc5N0MyIiwidHlwIjoiSldUIiwiY3R5IjoiSldUIn0.SGCXwa__o76pSJAlWrj_jQ_rEhQgfGkqBy2xTsppxKt7ItGlySIgOZ_IIfCd1RBK36zOFT6FS87GP1XE6LwemtKt_QYgtaKRV2ev-Xz_LNQeirOKOuZEOV16ZqGzZeKmzVVlXD4gimrBvwIvF-cSF2wqnsMBW-PeIzfe3Ph_KOjU_j5rYaWbenpH4toOxPpXk5ZTLrlnO7LVMCpZWiBb9UEVq42KmdAean0YRmR43ajkYm0cA2Zxsv0x6l5EP6r-6Vpto2hQ9fLawLb4Oa7VTHVMOfjG_2oTZIghUxvC-Dux0i7vdtXifSVMeTsk5102AiBQAkF5JhuDQtYeC5mPVg.qwP1kVqOwFz79bmB5-si8w.G8PsydDUfhOKngtZID5nhlN3X-T22pRwFu9yhR3VSORN_RiGxJjfei1zcIL8M-QGwtE9vCgckUByBqfK3C6RDvXkXp5TV1GqPSYYRr5xzOulA9QL9_aH0qwj1-9ExgYCeFYic5cR-7gjZqGAsLbnv6E-x5b6EJdLXILrsk5vRwdtV9v_5GrfGWfUuakVl968S-_YLgprgMwkCxJACS-MSrf25LKLGKKV031YkK5a1qBFvhZXp2W8Jw-RgqB74cby5E72HO94QiKcD0rwHXEbimQaPkbHwZ3ANEB6kCWZ6UeUfQsSo8qClTA7FRc0LYleC_7ikwyCKMwLKo4N897jCqX6ESzqoABCZBkuQOCME9MURMGU29ceRb8Fk35ECVMjgvTFJM9JS9mWLDm4rLqCWgZ9vgWEKFVP4inVo88pQWagQmTzbOgfVQYr-pPW3ggoDz6C7I6c04lZ9JSvmA5lFVrozCZYKnlJOukdIUpKW12Y6IePwkUM03G0K_3mAFi2eZQJqw-pG-ilMEnaCAcfTBzb2d9Y20eAcbAH8Nqm84VGDK2jIK6ibtUl_G2wiwJ0HnQGxeCJvl5zDx7by8DbZ7p-tmXNAOStSJscVp-gL1y3zkFUpOpnkqWgyvsplZgIB7mMd_5X10_pfiL1ETZvV0JXk6fJs2F3v7OJ9cE1i2jps433tHg79WuU1LT-_ei0NoD41UdaHy8HM4zWykjuvxFh8dJz53dhwr_vDGmvcmiADKaohKryngQnp9_2sSCt03zHi9aaVBVvodroh7CpOGhBxEF_7nLnetSPJSALRTGa8HWaix1hu0Eya3GLpVAH2zfuwQ_7c0ENy41KOVKLhXHpiAp_e4CD74XgWsXYIMuThfdposfKIkMNgXAqWRtRMbROprBuh_xpnMYVV-1yRDrEis_EUaU2AAKongTdj48Oe9XQUKr-SdmhrFbtiRDV.HSRJkASCAPrXzU1W-0jMIg";

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Jwt);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                //Debugger.Break(); // ← breakpoint: right before the API call is sent
                HttpResponseMessage httpResponse = _httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<TResponse>(content);
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
