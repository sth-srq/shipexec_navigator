using PSI.Sox;
using PSI.Sox.Data;
using PSI.Sox.Wcf.Administration;
using PSI.Sox.Wcf.Authentication;
using ShipExecNavigator.Model;
using ShipExecNavigator.BusinessLogic.CompanyBuilder;
using ShipExecNavigator.BusinessLogic.EntityComparison;
using ShipExecNavigator.BusinessLogic.RequestGeneration;
using ShipExecNavigator.BusinessLogic.Tools;
using ShipExecNavigator.BusinessLogic.ResponseModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ShipExecNavigator.BusinessLogic
{
    public class AppManager
    {
        private JWTManager _jwtManager = new JWTManager();
        private CompanyExportRequestGenerator _companyExportRequestGenerator;
        private string _adminUrl;
        private string _jwtFilePath = "";
        private string _jwtString = "";
        private Guid _companyId = Guid.NewGuid();
        private string _companyName = "";

        public AppManager(Guid companyGuid, string companyName, string jwtFilePath, string adminUrl)
        {
            _companyId = companyGuid;
            _companyName = companyName;
            _jwtFilePath = jwtFilePath;
            _adminUrl = adminUrl;
        }

        public AppManager(string jwtString, string adminUrl)
        {
            _jwtString = jwtString;
            _adminUrl = adminUrl;
        }

        public void SetCompany(Guid companyId, string companyName)
        {
            _companyId = companyId;
            _companyName = companyName;
        }

        public string GetAccessToken()
        {
            if (!string.IsNullOrEmpty(_jwtString))
                return _jwtManager.GetAccessToken(_jwtString);
            var jwtFile = File.ReadAllText(_jwtFilePath);
            var accessToken = _jwtManager.GetAccessToken(jwtFile);
            return accessToken;
        }

        public void RefreshJwt()
        {
            var jwtFile = File.ReadAllText(_jwtFilePath);
            var currentJwt = _jwtManager.ConvertToObject(jwtFile);
            string json = "{\"refresh_token\":\"" + currentJwt.refresh_token + "\"}";

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetRefreshToken"))
            {
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var newJwt = JsonHelper.Deserialize<JWT>(content);
                File.WriteAllText(_jwtFilePath, JsonHelper.Serialize(newJwt));
            }
        }

        public List<Company> GetCompanies()
        {
            var accessToken = GetAccessToken();
            var request = new GetCompaniesRequest();

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetCompanies"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                GetCompaniesResponse response = JsonHelper.Deserialize<GetCompaniesResponse>(content);
                return response?.Companies ?? new List<Company>();
            }
        }

        public string GetCompanyXmlString(string path = "", string companyName = "", HashSet<string>? loadedSections = null)
        {
            var accessToken = GetAccessToken();
            var gen = new CompanyExportRequestGenerator(_adminUrl, _companyId, accessToken);
            return gen.GetLatestCompanyXmlString(path, companyName.Replace(" ", ""), loadedSections);
        }

        public List<Shipper> GetShippers()
        {
            var accessToken = GetAccessToken();
            var gen = new ShipperRequestGenerator(_adminUrl, _companyId, accessToken);
            var response = gen.Get();
            return response?.Shippers ?? new List<Shipper>();
        }

        public List<User> GetUsers()
        {
            var accessToken = GetAccessToken();
            var request = new GetUsersRequest
            {
                CompanyId = _companyId,
                SearchCriteria = new SearchCriteria
                {
                    Skip = 0,
                    Take = int.MaxValue,
                    WhereClauses = new List<WhereClause>
                    {
                        new WhereClause { FieldName = "Email", Operator = SearchOperator.Contains }
                    },
                    OrderByClauses = new List<OrderByClause>
                    {
                        new OrderByClause { FieldName = "Email", FieldType = PSI.Sox.Data.FieldType.String }
                    }
                }
            };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "GetUsers"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                GetUsersResponse response = JsonHelper.Deserialize<GetUsersResponse>(content);
                return response?.Users ?? new List<User>();
            }
        }

        public User GetUserDetail(Guid userId)
        {
            var accessToken = GetAccessToken();
            var request = new PSI.Sox.Wcf.Administration.GetUserRequest
            {
                UserId = userId,
            };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "GetUser"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                PSI.Sox.Wcf.Administration.GetUserResponse response = JsonHelper.Deserialize<PSI.Sox.Wcf.Administration.GetUserResponse>(content);
                return response?.User;
            }
        }

        public void UpdateUserPermissions(User user, List<Permission> permissions)
        {
            var accessToken = GetAccessToken();
            user.Permissions = permissions;
            var request = new PSI.Sox.Wcf.Authentication.UpdateUserRequest { User = user };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "UpdateUser"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public List<Permission> GetPermissions(Guid userId)
        {
            var accessToken = GetAccessToken();
            var request = new GetPermissionsRequest
            {
                UserContext = new PSI.Sox.UserContext { UserId = userId }
            };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "GetPermissions"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                GetPermissionsResponse response = JsonHelper.Deserialize<GetPermissionsResponse>(content);
                return response?.Permissions ?? new List<Permission>();
            }
        }

        public List<Role> GetRoles()
        {
            var accessToken = GetAccessToken();
            var request = new PSI.Sox.Wcf.Authentication.GetRolesRequest
            {
                UserContext = new PSI.Sox.UserContext { CompanyId = _companyId }
            };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "GetRoles"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                PSI.Sox.Wcf.Authentication.GetRolesResponse response = JsonHelper.Deserialize<PSI.Sox.Wcf.Authentication.GetRolesResponse>(content);
                return response?.Roles ?? new List<Role>();
            }
        }

        public void UpdateUserRoles(User user, List<Role> roles)
        {
            var accessToken = GetAccessToken();
            user.Roles = roles;
            var request = new PSI.Sox.Wcf.Authentication.UpdateUserRequest { User = user };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "UpdateUser"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public void UpdateUser(User user)
        {
            var accessToken = GetAccessToken();
            var request = new PSI.Sox.Wcf.Authentication.UpdateUserRequest { User = user };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "UpdateUser"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();
            }
        }

        public Tuple<List<Variance>, List<RequestBaseWithURL>> GetVariancesAndRequests(string originalXml, string modifiedXml)
        {
            var existingCompany = CompanyExtractor.GetCompany(originalXml);
            var modifiedCompany = CompanyExtractor.GetCompany(modifiedXml);

            var accessToken = GetAccessToken();
            var manager = new CompanyBuilderManager(_adminUrl, _companyId, accessToken);

            var variances = manager.GetVariances(existingCompany, modifiedCompany);
            var requests = manager.GetRequests(modifiedCompany, variances);

            return Tuple.Create(variances, requests);
        }

        public void WriteCompanyFilesWithIds(string outputFilePathWithFileIds)
        {
            string originalFileName = _companyName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xml";
            string modifiedFileName = "mod." + _companyName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xml";

            bool forceOutput = false;
            if (!File.Exists(outputFilePathWithFileIds) || forceOutput)
            {
                var accessToken = GetAccessToken();
                var companyExportRequestGenerator = new CompanyExportRequestGenerator(_adminUrl, _companyId, accessToken);
                var company = companyExportRequestGenerator.GetLatestCompanyAndWriteToFileWithIds(outputFilePathWithFileIds, originalFileName, modifiedFileName);
            }
        }

        public void MakeChanges(string outputFilePathWithIds, string originalFileName, string modifiedFileName)
        {
            // 2. Pull companies out of files
            var existingCompany = CompanyExtractor.GetFile_TestOutput(Path.Combine(outputFilePathWithIds, originalFileName));
            var modifiedCompany = CompanyExtractor.GetFile_ModifiedCompany(Path.Combine(outputFilePathWithIds, modifiedFileName));

            // (Optional)
            //GetModifiedFileThroughCode(ref modifiedCompany);


            // 3. Get variances
            string accessToken = GetAccessToken();
            CompanyBuilderManager companyBuilderManager = new CompanyBuilderManager(_adminUrl, _companyId, accessToken);

            var variances = companyBuilderManager.GetVariances(existingCompany, modifiedCompany);

            // 4. Get Requests from variances

            var requests = companyBuilderManager.GetRequests(modifiedCompany, variances);


            // 5. Run scripts

            //var responses = companyBuilderManager.ApplyRequests(requests);
        }

        public void MakeChanges(string originalXml, string modifiedXml)
        {
            var existingCompany = CompanyExtractor.GetCompany(originalXml);
            var modifiedCompany = CompanyExtractor.GetCompany(modifiedXml);

            string accessToken = GetAccessToken();
            CompanyBuilderManager companyBuilderManager = new CompanyBuilderManager(_adminUrl, _companyId, accessToken);

            var variances = companyBuilderManager.GetVariances(existingCompany, modifiedCompany);

            var requests = companyBuilderManager.GetRequests(modifiedCompany, variances);

            //var responses = companyBuilderManager.ApplyRequests(requests);
        }

        public List<RequestBaseWithURL> MakeChanges(string modifiedXml, List<Variance> variances)
        {
            var modifiedCompany = CompanyExtractor.GetCompany(modifiedXml);

            string accessToken = GetAccessToken();
            CompanyBuilderManager companyBuilderManager = new CompanyBuilderManager(_adminUrl, _companyId, accessToken);

            return companyBuilderManager.GetRequests(modifiedCompany, variances);
        }

        public List<ApplyChangeResult> ApplyChanges(string modifiedXml, List<Variance> variances)
        {
            var requests = MakeChanges(modifiedXml, variances);

            string accessToken = GetAccessToken();
            CompanyBuilderManager companyBuilderManager = new CompanyBuilderManager(_adminUrl, _companyId, accessToken);
            var responses = companyBuilderManager.ApplyRequests(requests);

            return requests.Zip(responses, (req, res) => new ApplyChangeResult
            {
                EntityName = req.EntityName,
                Operation  = req.IsAdd ? "Add" : req.IsDelete ? "Remove" : "Update",
                Endpoint   = req.Endpoint,
                Success    = res.ErrorCode == 0,
                Message    = res.ErrorMessage ?? string.Empty,
            }).ToList();
        }



        public void GetModifiedFileThroughCode(ref Company company)
        {
            company.Clients = new List<Client>
            {
                new Client
                {
                    CompanyId = _companyId,
                    AccessKey = Guid.NewGuid().ToString(),
                    Name = "Test1"
                },
                new Client
                {
                    CompanyId = _companyId,
                    AccessKey = Guid.NewGuid().ToString(),
                    Name = "Test2"
                },new Client
                {
                    CompanyId = _companyId,
                    AccessKey = Guid.NewGuid().ToString(),
                    Name = "Test3"
                },new Client
                {
                    CompanyId = _companyId,
                    AccessKey = Guid.NewGuid().ToString(),
                    Name = "Test4"
                },
            };

            var modShipper = company.Shippers.FirstOrDefault(x => x.Symbol == "MOD");
            if (modShipper != null)
            {
                modShipper.Address1 = "Changed in code";
            }

            foreach (var shipper in company.Shippers)
            {
                if (shipper != null)
                {
                    shipper.Address2 = "ADDRESS2_CHANGED!";
                }
            }
        }

        public Guid GetCurrentCompanyId() => _companyId;
        public string GetCurrentCompanyName() => _companyName;
        public string AdminUrl => _adminUrl;

        /// <summary>
        /// Returns the Company object from the GetCompany API without populating
        /// entity collections (Shippers, Clients, etc.).
        /// Used for building the skeleton tree with lazy-loadable category nodes.
        /// </summary>
        public Company GetCompanyBase()
        {
            var accessToken = GetAccessToken();
            var request = new PSI.Sox.Wcf.Administration.GetCompanyRequest { Id = _companyId };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetCompany"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<GetCompanyResponse>(content);
                return response.Company;
            }
        }

        public List<Client> GetClients()
        {
            var accessToken = GetAccessToken();
            var gen = new ClientRequestGenerator(_adminUrl, _companyId, accessToken);
            var response = gen.Get();
            return response?.Clients ?? new List<Client>();
        }

        public void AddClient(Client client)
        {
            var accessToken = GetAccessToken();
            var gen = new ClientRequestGenerator(_adminUrl, _companyId, accessToken);
            var response = gen.Add(client);
            if (response?.ErrorCode != 0)
                throw new Exception(response?.ErrorMessage ?? "Failed to add client.");
        }

        public List<PSI.Sox.Site> GetSites(Guid companyId)
        {
            var accessToken = GetAccessToken();
            var request = new PSI.Sox.Wcf.GetSitesRequest
            {
                CompanyId = companyId,
                SearchCriteria = new SearchCriteria
                {
                    Skip = 0,
                    Take = int.MaxValue,
                    WhereClauses = new List<WhereClause>
                    {
                        new WhereClause { FieldName = "Name", Operator = SearchOperator.Contains }
                    },
                    OrderByClauses = new List<OrderByClause>
                    {
                        new OrderByClause { FieldName = "Name", FieldType = PSI.Sox.Data.FieldType.String }
                    }
                }
            };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetSites"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<PSI.Sox.Wcf.GetSitesResponse>(content);
                return response?.Sites ?? new List<PSI.Sox.Site>();
            }
        }

        public List<Profile> GetProfiles()
        {
            var accessToken = GetAccessToken();
            var request = new GetCompanyProfilesRequest
            {
                CompanyId = _companyId,
                SearchCriteria = new SearchCriteria
                {
                    WhereClauses = new List<WhereClause>(),
                    OrderByClauses = new List<OrderByClause>()
                }
            };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetCompanyProfiles"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<GetProfilesResponse>(content);
                return response?.Profiles ?? new List<Profile>();
            }
        }

        public Profile GetFullProfile(int profileId)
        {
            var accessToken = GetAccessToken();
            var fetcher = new ProfileDetailFetcher(_adminUrl, _companyId, profileId, accessToken);
            var response = fetcher.Fetch();
            return response?.Profile;
        }

        public List<Template> GetTemplatesForCompany(Guid companyId)
        {
            var accessToken = GetAccessToken();
            var request = new GetCompanyTemplatesRequest
            {
                CompanyId = companyId,
                SearchCriteria = new SearchCriteria
                {
                    WhereClauses = new List<WhereClause>(),
                    OrderByClauses = new List<OrderByClause>()
                }
            };

            //System.Diagnostics.Debugger.Break(); // breakpoint before HTTP call to GetCompanyTemplates

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetCompanyTemplates"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<GetTemplatesResponse>(content);
                return response?.Templates ?? new List<Template>();
            }
        }

        public string GetApplicationLogsJson(DateTime startDate, DateTime endDate)
        {
            var accessToken = GetAccessToken();
            var requestJson = string.Format(
                "{{\"SearchCriteria\":{{\"Skip\":0,\"Take\":1000," +
                "\"WhereClauses\":[" +
                "{{\"FieldName\":\"StartLogDate\",\"FieldValue\":\"{0}\",\"Operator\":3}}," +
                "{{\"FieldName\":\"EndLogDate\",\"FieldValue\":\"{1}\",\"Operator\":4}}," +
                "{{\"FieldName\":\"CompanyId\",\"FieldValue\":\"{2}\",\"Operator\":0}}" +
                "]," +
                "\"OrderByClauses\":[{{\"FieldName\":\"LogDate\",\"FieldType\":3}}]" +
                "}}}}",
                startDate.ToString("yyyy-M-d HH:mm:ss.fff"),
                endDate.ToString("yyyy-M-d HH:mm:ss.fff"),
                _companyId);

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetApplicationLogs"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();
                return httpResponse.Content.ReadAsStringAsync().Result;
            }
        }

        public string GetSecurityLogsJson(DateTime startDate, DateTime endDate)
        {
            var accessToken = GetAccessToken();
            var requestJson = string.Format(
                "{{\"SearchCriteria\":{{\"Skip\":0,\"Take\":1000," +
                "\"WhereClauses\":[" +
                "{{\"FieldName\":\"StartLogDate\",\"FieldValue\":\"{0}\",\"Operator\":3}}," +
                "{{\"FieldName\":\"EndLogDate\",\"FieldValue\":\"{1}\",\"Operator\":4}}," +
                "{{\"FieldName\":\"CompanyId\",\"FieldValue\":\"{2}\",\"Operator\":0}}" +
                "]," +
                "\"OrderByClauses\":[{{\"FieldName\":\"LogDate\",\"FieldType\":3}}]" +
                "}}}}",
                startDate.ToString("yyyy-M-d HH:mm:ss.fff"),
                endDate.ToString("yyyy-M-d HH:mm:ss.fff"),
                _companyId);

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetSecurityLogs"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();
                return httpResponse.Content.ReadAsStringAsync().Result;
            }
        }

        public Guid CreateUser(User user)
        {
            var accessToken = GetAccessToken();
            user.CompanyId = _companyId;

            bool generatePassword = string.IsNullOrEmpty(user.PasswordHash);
            if (!generatePassword)
                ValidatePasswordComplexity(user.PasswordHash);

            if (user.Roles == null || user.Roles.Count == 0)
                throw new ArgumentException("At least one role must be assigned to the user.", "user");

            var request = new PSI.Sox.Wcf.Authentication.CreateUserRequest
            {
                UserName = user.UserName,
                Password = user.PasswordHash,
                Email = user.Email,
                EnterpriseId = user.EnterpriseId,
                CompanyId = _companyId,
                SiteId = user.SiteId,
                ProfileId = user.ProfileId,
                Permissions = user.Permissions,
                Roles = user.Roles,
                PasswordExpired = user.PasswordExpired,
                Address = user.Address,
                DefaultConfiguration = user.DefaultConfiguration,
                GeneratePassword = generatePassword,
                SendEmailNotification = false,
                IsSsoEnabled = false,
                UserContext = new PSI.Sox.UserContext { CompanyId = _companyId }
            };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "CreateUser"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<PSI.Sox.Wcf.Authentication.CreateUserResponse>(content);
                if (response?.ErrorCode != 0)
                    throw new Exception(response?.ErrorMessage ?? "Failed to create user.");
                return response.Id;
            }
        }

        private static void ValidatePasswordComplexity(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("A password is required to create a user.");

            var errors = new System.Text.StringBuilder();

            if (!password.Any(char.IsUpper))
                errors.AppendLine("Password must contain at least one uppercase letter ('A'-'Z').");
            if (!password.Any(char.IsDigit))
                errors.AppendLine("Password must contain at least one digit ('0'-'9').");
            if (password.All(c => char.IsLetterOrDigit(c)))
                errors.AppendLine("Password must contain at least one non-alphanumeric character (e.g. !, @, #).");

            if (errors.Length > 0)
                throw new ArgumentException(errors.ToString().TrimEnd());
        }

        // ── Apply Variance ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a single variance to the live ShipExec server by routing it to the
        /// correct entity endpoint (e.g. UpdateShipper, RemoveClient, AddSite).
        /// </summary>
        public ApplyChangeResult ApplyVarianceEntry(Guid companyId, Variance variance)
        {
            var generator = new CompanyRequestGenerator(_adminUrl, GetAccessToken());

            


            return generator.ApplyVariance(variance, companyId);
        }
    }
}
