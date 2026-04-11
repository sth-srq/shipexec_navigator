using PSI.Sox;
using PSI.Sox.Wcf;
using PSI.Sox.Wcf.Administration;
using ShipExecNavigator.BusinessLogic.CompanyBuilder;
using ShipExecNavigator.BusinessLogic.EntityComparison;
using ShipExecNavigator.BusinessLogic.ResponseModel;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ShipExecNavigator.BusinessLogic.RequestGeneration
{
    public class CompanyRequestGenerator
    {
        private readonly string _adminUrl;
        private readonly string _jwt;

        public CompanyRequestGenerator(string adminUrl, string jwt)
        {
            _adminUrl = adminUrl;
            _jwt = jwt;
        }

        /// <summary>
        /// Calls UpdateCompany with only scalar properties — child collections
        /// (Sites, Profiles, Clients, etc.) each have their own dedicated endpoints.
        /// </summary>
        public ResponseBase Update(Company company)
        {
            var request = new UpdateCompanyRequest
            {
                Company = company,
                EnterpriseId = company.EnterpriseId
            };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "UpdateCompany"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
                var json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var webResult = httpClient.SendAsync(requestMessage).Result;
                string resultContent = webResult.Content.ReadAsStringAsync().Result;
                return JsonHelper.Deserialize<UpdateCompanyResponse>(resultContent);
            }
        }

        // ── Apply Variance ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a single <see cref="Variance"/> to the live ShipExec server by routing
        /// it to the correct entity endpoint (e.g. UpdateShipper, RemoveClient, AddSite).
        /// </summary>
        public ApplyChangeResult ApplyVariance(Variance variance, Guid companyId)
        {
            var changeType = variance.IsAdd ? "Added" : variance.IsRemove ? "Removed" : "Modified";
            try
            {
                var manager  = new CompanyBuilderManager(_adminUrl, companyId, _jwt);
                var response = manager.ApplyVariance(variance);

                return new ApplyChangeResult
                {
                    Success    = response.ErrorCode == 0,
                    Message    = response.ErrorMessage ?? string.Empty,
                    EntityPath = variance.EntityName,
                    ChangeType = changeType,
                };
            }
            catch (Exception ex)
            {
                return new ApplyChangeResult
                {
                    Success    = false,
                    Message    = ex.Message,
                    EntityPath = variance.EntityName,
                    ChangeType = changeType,
                };
            }
        }

            }
        }
