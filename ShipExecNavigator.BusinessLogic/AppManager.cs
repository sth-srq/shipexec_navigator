using PSI.Sox;
using PSI.Sox.Data;
using PSI.Sox.Wcf.Administration;
using PSI.Sox.Wcf.Authentication;
using Microsoft.Extensions.Logging;
using ShipExecNavigator.BusinessLogic.Logging;
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
    /// <summary>
    /// Central orchestrator for all ShipExec API interactions.
    /// <para>
    /// <see cref="AppManager"/> is the single entry-point through which the Blazor
    /// front-end (via <c>ShipExecService</c>) communicates with the ShipExec
    /// Management Studio REST API.  It owns three high-level responsibilities:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <term>Authentication</term>
    ///     <description>
    ///       Reads a JWT from either an in-memory string or a file path, extracts the
    ///       <c>access_token</c>, and attaches it as a Bearer header on every outbound
    ///       HTTP request.  Token refresh is available via <see cref="RefreshJwt"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Data retrieval</term>
    ///     <description>
    ///       Fetches companies, shippers, users, sites, profiles, and the full company
    ///       XML configuration from the Management Studio API, converting raw JSON
    ///       responses into typed PSI.Sox model objects.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Variance &amp; apply pipeline</term>
    ///     <description>
    ///       Deserialises an original and a modified XML document into <c>Company</c>
    ///       object graphs, delegates variance detection to
    ///       <see cref="CompanyBuilderManager.GetVariances"/>, converts variances into
    ///       typed API requests, and drives
    ///       <see cref="CompanyBuilderManager.ApplyRequests"/> to push changes back to
    ///       ShipExec.  Results are surfaced as
    ///       <see cref="ResponseModel.ApplyChangeResult"/> items so the caller can
    ///       display per-entity success/failure.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// <b>Construction:</b> use the <c>(string jwtJson, string adminUrl)</c> overload when
    /// the JWT is available as a JSON string (the typical Blazor path) and the
    /// <c>(Guid, string, string, string)</c> overload when loading from a file (legacy desktop path).
    /// </para>
    /// <para>
    /// <b>Thread safety:</b> <see cref="AppManager"/> is <em>not</em> thread-safe.
    /// The Blazor application registers <c>ShipExecService</c> (which owns one
    /// <see cref="AppManager"/> instance) as a <c>Scoped</c> service, so each
    /// SignalR circuit gets its own instance.
    /// </para>
    /// </summary>
    public class AppManager
    {
        // ── Logging ──────────────────────────────────────────────────────────────
        private readonly ILogger<AppManager> _logger = LoggerProvider.CreateLogger<AppManager>();

        // ── Authentication ───────────────────────────────────────────────────────
        /// <summary>Handles JWT parsing and access-token extraction.</summary>
        private JWTManager _jwtManager = new JWTManager();

        /// <summary>
        /// Lazily initialised reference kept here for file-based export flows
        /// that need to write the raw XML to disk before loading it.
        /// </summary>
        private CompanyExportRequestGenerator _companyExportRequestGenerator;

        // ── API coordinates ───────────────────────────────────────────────────────
        /// <summary>Base URL of the ShipExec Management Studio Administration API endpoint.</summary>
        private string _adminUrl;

        // ── JWT source (one of the two is set; the other stays empty) ────────────
        /// <summary>
        /// Absolute path to the JWT JSON file used by the legacy file-based constructor.
        /// When populated, every request reads and parses this file to get a fresh token.
        /// </summary>
        private string _jwtFilePath = "";

        /// <summary>
        /// Raw JWT JSON string supplied at construction time.
        /// Used by the standard Blazor flow where the JWT arrives from the UI.
        /// </summary>
        private string _jwtString = "";

        // ── Company scope ─────────────────────────────────────────────────────────
        /// <summary>GUID of the company currently being operated on.</summary>
        private Guid _companyId = Guid.NewGuid();

        /// <summary>Display name of the currently selected company (used in file names and log messages).</summary>
        private string _companyName = "";

        /// <summary>
        /// Initialises <see cref="AppManager"/> from a file-based JWT — used by legacy desktop flows.
        /// </summary>
        /// <param name="companyGuid">GUID of the pre-selected company.</param>
        /// <param name="companyName">Display name of the pre-selected company.</param>
        /// <param name="jwtFilePath">Absolute path to a JSON file containing the JWT payload.</param>
        /// <param name="adminUrl">Base URL of the Management Studio Administration Service.</param>
        public AppManager(Guid companyGuid, string companyName, string jwtFilePath, string adminUrl)
        {
            _companyId = companyGuid;
            _companyName = companyName;
            _jwtFilePath = jwtFilePath;
            _adminUrl = adminUrl;
        }

        /// <summary>
        /// Initialises <see cref="AppManager"/> from an in-memory JWT JSON string —
        /// the standard path used by the Blazor application after a successful login.
        /// </summary>
        /// <param name="jwtString">Raw JWT JSON (the full token payload, not just the access_token).</param>
        /// <param name="adminUrl">Base URL of the Management Studio Administration Service.</param>
        public AppManager(string jwtString, string adminUrl)
        {
            _jwtString = jwtString;
            _adminUrl = adminUrl;
        }

        /// <summary>
        /// Switches the active company context. Subsequent API calls will target
        /// <paramref name="companyId"/> / <paramref name="companyName"/> until changed again.
        /// </summary>
        /// <param name="companyId">The GUID of the company to select.</param>
        /// <param name="companyName">The display name of the company (used for file naming).</param>
        public void SetCompany(Guid companyId, string companyName)
        {
            _logger.LogTrace(">> SetCompany({CompanyId}, {CompanyName})", companyId, companyName);
            _companyId = companyId;
            _companyName = companyName;
            _logger.LogTrace("<< SetCompany");
        }

        /// <summary>
        /// Extracts the current Bearer access token from the stored JWT.
        /// <para>
        /// When <c>_jwtString</c> is populated (Blazor flow) the token is decoded
        /// in-memory.  When only <c>_jwtFilePath</c> is set (legacy desktop flow)
        /// the file is read on every call so token refreshes are picked up automatically.
        /// </para>
        /// </summary>
        /// <returns>The raw access-token string to be placed in the <c>Authorization: Bearer</c> header.</returns>
        public string GetAccessToken()
        {
            _logger.LogTrace(">> GetAccessToken");
            string token;
            if (!string.IsNullOrEmpty(_jwtString))
                token = _jwtManager.GetAccessToken(_jwtString);
            else
            {
                var jwtFile = File.ReadAllText(_jwtFilePath);
                token = _jwtManager.GetAccessToken(jwtFile);
            }
            _logger.LogTrace("<< GetAccessToken → [token length {Len}]", token?.Length ?? 0);
            return token;
        }

        /// <summary>
        /// Exchanges the stored refresh token for a new JWT and writes it back to
        /// <c>_jwtFilePath</c>.  Only available when the file-based constructor was used.
        /// </summary>
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

        /// <summary>
        /// Retrieves all companies accessible with the current JWT from the Management Studio API.
        /// The result is used to populate the company-selection list on the Connect dialog.
        /// </summary>
        /// <returns>A list of <see cref="Company"/> objects. Returns an empty list when the API returns no data.</returns>
        public List<Company> GetCompanies()
        {
            _logger.LogTrace(">> GetCompanies | AdminUrl={AdminUrl}", _adminUrl);
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
                var result = response?.Companies ?? new List<Company>();
                _logger.LogTrace("<< GetCompanies → {Count} companies", result.Count);
                return result;
            }
        }

        /// <summary>
        /// Fetches the full company configuration as a serialised XML string.
        /// Internally delegates to <see cref="CompanyExportRequestGenerator.GetLatestCompanyXmlString"/>
        /// which calls multiple Management Studio endpoints (GetCompany, GetCompanyProfiles,
        /// GetShippers, etc.) and assembles them into a single <c>Company</c> XML document.
        /// The result is written to a temporary file and then read back as a string.
        /// </summary>
        /// <param name="path">
        /// Optional directory path for the temporary file.
        /// Defaults to <see cref="Path.GetTempPath"/> when empty.
        /// </param>
        /// <param name="companyName">
        /// Company display name — spaces are stripped and used as part of the temp file name.
        /// </param>
        /// <param name="loadedSections">
        /// Optional allow-list of section names (e.g. <c>{"Shippers","Clients"}</c>).
        /// When <see langword="null"/> every section is populated.
        /// </param>
        /// <returns>The complete company XML as a UTF-8 string.</returns>
        public string GetCompanyXmlString(string path = "", string companyName = "", HashSet<string>? loadedSections = null)
        {
            _logger.LogTrace(">> GetCompanyXmlString | CompanyId={CompanyId} Path={Path}", _companyId, path);
            var accessToken = GetAccessToken();
            var gen = new CompanyExportRequestGenerator(_adminUrl, _companyId, accessToken);
            var result = gen.GetLatestCompanyXmlString(path, companyName.Replace(" ", ""), loadedSections);
            _logger.LogTrace("<< GetCompanyXmlString → {XmlLength} chars", result?.Length ?? 0);
            return result;
        }

        /// <summary>
        /// Retrieves all <see cref="Shipper"/> records for the currently selected company.
        /// </summary>
        public List<Shipper> GetShippers()
        {
            _logger.LogTrace(">> GetShippers | CompanyId={CompanyId}", _companyId);
            var accessToken = GetAccessToken();
            var gen = new ShipperRequestGenerator(_adminUrl, _companyId, accessToken);
            var response = gen.Get();
            var result = response?.Shippers ?? new List<Shipper>();
            _logger.LogTrace("<< GetShippers → {Count} shippers", result.Count);
            return result;
        }

        /// <summary>
        /// Retrieves all <see cref="User"/> records for the currently selected company,
        /// ordered by email address.  The search takes <c>int.MaxValue</c> records to
        /// ensure the full user list is returned in one call.
        /// </summary>
        public List<User> GetUsers()
        {
            _logger.LogTrace(">> GetUsers | CompanyId={CompanyId}", _companyId);
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
                var usersResult = response?.Users ?? new List<User>();
                _logger.LogTrace("<< GetUsers → {Count} users", usersResult.Count);
                return usersResult;
            }
        }

        public User GetUserDetail(Guid userId)
        {
            _logger.LogTrace(">> GetUserDetail({UserId})", userId);
            var accessToken = GetAccessToken();
            var request = new PSI.Sox.Wcf.Administration.GetUserRequest { UserId = userId };
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
                _logger.LogTrace("<< GetUserDetail → {UserName}", response?.User?.UserName);
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

        public void DeleteUser(Guid userId)
        {
            _logger.LogTrace(">> DeleteUser({UserId})", userId);
            var accessToken = GetAccessToken();
            var request = new PSI.Sox.Wcf.Authentication.RemoveUserRequest
            {
                Id = userId,
                UserContext = new PSI.Sox.UserContext { CompanyId = _companyId }
            };
            var userManagerUrl = _adminUrl.Replace("AdministrationService", "UserManagerService");

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, userManagerUrl + "RemoveUser"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();
                _logger.LogTrace("<< DeleteUser({UserId})", userId);
            }
        }

        /// <summary>
        /// Computes the set of entity-level differences between two XML documents and
        /// converts those differences into typed API request objects ready to be applied.
        /// <para>
        /// The method deserialises both XML strings into <c>Company</c> object graphs via
        /// <see cref="CompanyExtractor"/>, then delegates to
        /// <see cref="CompanyBuilderManager.GetVariances"/> and
        /// <see cref="CompanyBuilderManager.GetRequests"/> to build the full diff.
        /// </para>
        /// </summary>
        /// <param name="originalXml">Serialised XML of the company as it existed before editing.</param>
        /// <param name="modifiedXml">Serialised XML of the company after the user's edits.</param>
        /// <returns>
        /// A tuple of:
        /// <list type="bullet">
        ///   <item><see cref="Variance"/> list — one entry per changed entity, with
        ///         <c>OriginalObject</c> / <c>NewObject</c> payloads for display.</item>
        ///   <item><see cref="RequestBaseWithURL"/> list — HTTP request descriptors
        ///         ready for <see cref="ApplyChanges(string,List{Variance})"/>.</item>
        /// </list>
        /// </returns>
        public Tuple<List<Variance>, List<RequestBaseWithURL>> GetVariancesAndRequests(string originalXml, string modifiedXml)
        {
            _logger.LogTrace(">> GetVariancesAndRequests | OrigLen={OrigLen} ModLen={ModLen}",
                originalXml?.Length ?? 0, modifiedXml?.Length ?? 0);
            var existingCompany = CompanyExtractor.GetCompany(originalXml);
            var modifiedCompany = CompanyExtractor.GetCompany(modifiedXml);

            var accessToken = GetAccessToken();
            var manager = new CompanyBuilderManager(_adminUrl, _companyId, accessToken);

            var variances = manager.GetVariances(existingCompany, modifiedCompany);
            var requests = manager.GetRequests(modifiedCompany, variances);

            _logger.LogTrace("<< GetVariancesAndRequests → {Variances} variances, {Requests} requests",
                variances.Count, requests.Count);
            return Tuple.Create(variances, requests);
        }

        /// <summary>
        /// Writes the current company XML to a timestamped file in
        /// <paramref name="outputFilePathWithFileIds"/> if it does not already exist.
        /// Intended for legacy desktop diagnostic flows only.
        /// </summary>
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

        /// <summary>
        /// File-based variant of the diff/apply pipeline — loads original and modified
        /// company XML from disk, computes variances, and builds API requests.
        /// The resulting requests are <em>not</em> applied; this overload is intended
        /// for diagnostic or preview scenarios.
        /// </summary>
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
            _logger.LogTrace(">> ApplyChanges | VarianceCount={Count}", variances?.Count ?? 0);
            var requests = MakeChanges(modifiedXml, variances);

            string accessToken = GetAccessToken();
            CompanyBuilderManager companyBuilderManager = new CompanyBuilderManager(_adminUrl, _companyId, accessToken);
            var responses = companyBuilderManager.ApplyRequests(requests);

            var result = requests.Zip(responses, (req, res) => new ApplyChangeResult
            {
                EntityName = req.EntityName,
                Operation  = req.IsAdd ? "Add" : req.IsDelete ? "Remove" : "Update",
                Endpoint   = req.Endpoint,
                Success    = res.ErrorCode == 0,
                Message    = res.ErrorMessage ?? string.Empty,
            }).ToList();
            _logger.LogTrace("<< ApplyChanges → {Count} results", result.Count);
            return result;
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

        public Guid GetCurrentCompanyId() { _logger.LogTrace(">> GetCurrentCompanyId → {Id}", _companyId); return _companyId; }
        public string GetCurrentCompanyName() { _logger.LogTrace(">> GetCurrentCompanyName → {Name}", _companyName); return _companyName; }
        public string AdminUrl => _adminUrl;

        /// <summary>
        /// Returns the Company object from the GetCompany API without populating
        /// entity collections (Shippers, Clients, etc.).
        /// Used for building the skeleton tree with lazy-loadable category nodes.
        /// </summary>
        public Company GetCompanyBase()
        {
            _logger.LogTrace(">> GetCompanyBase | CompanyId={CompanyId}", _companyId);
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
                _logger.LogTrace("<< GetCompanyBase → {CompanyName}", response?.Company?.Name);
                return response.Company;
            }
        }

        public List<Client> GetClients()
        {
            _logger.LogTrace(">> GetClients | CompanyId={CompanyId}", _companyId);
            var accessToken = GetAccessToken();
            var gen = new ClientRequestGenerator(_adminUrl, _companyId, accessToken);
            var response = gen.Get();
            var result = response?.Clients ?? new List<Client>();
            _logger.LogTrace("<< GetClients → {Count} clients", result.Count);
            return result;
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
            _logger.LogTrace(">> GetSites({CompanyId})", companyId);
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
                var sitesResult = response?.Sites ?? new List<PSI.Sox.Site>();
                _logger.LogTrace("<< GetSites → {Count} sites", sitesResult.Count);
                return sitesResult;
            }
        }

        public List<Profile> GetProfiles()
        {
            _logger.LogTrace(">> GetProfiles | CompanyId={CompanyId}", _companyId);
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
                var profilesResult = response?.Profiles ?? new List<Profile>();
                _logger.LogTrace("<< GetProfiles → {Count} profiles", profilesResult.Count);
                return profilesResult;
            }
        }

        /// <summary>
        /// Returns fully-hydrated profiles (including nested collections like Shippers,
        /// Carriers, etc.). Gets the profile list first, then fetches full detail for each.
        /// </summary>
        public List<Profile> GetFullProfiles()
        {
            _logger.LogTrace(">> GetFullProfiles | CompanyId={CompanyId}", _companyId);
            var summaryProfiles = GetProfiles();
            var result = new List<Profile>(summaryProfiles.Count);

            foreach (var summary in summaryProfiles)
            {
                try
                {
                    var full = GetFullProfile(summary.Id);
                    result.Add(full ?? summary);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GetFullProfiles: failed to load detail for Profile {ProfileId}, using summary", summary.Id);
                    result.Add(summary);
                }
            }

            _logger.LogTrace("<< GetFullProfiles → {Count} profiles", result.Count);
            return result;
        }

        public Profile GetFullProfile(int profileId)
        {
            _logger.LogTrace(">> GetFullProfile({ProfileId})", profileId);
            var accessToken = GetAccessToken();
            var fetcher = new ProfileDetailFetcher(_adminUrl, _companyId, profileId, accessToken);
            var response = fetcher.Fetch();
            _logger.LogTrace("<< GetFullProfile → {ProfileName}", response?.Profile?.Name);
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

        public List<ClientBusinessRule> GetClientBusinessRulesForCompany(Guid companyId)
        {
            // Set company context so GetProfiles() targets the correct company.
            SetCompany(companyId, string.Empty);

            // Collect every CBR ID that is actually assigned to a profile.
            var profileCbrIds = GetProfiles()
                .Select(p => p.ClientBusinessRuleId)
                .Where(id => id != 0)
                .ToHashSet();

            var accessToken = GetAccessToken();
            var request = new GetClientBusinessRulesRequest
            {
                CompanyId = companyId,
                SearchCriteria = new SearchCriteria
                {
                    WhereClauses = new List<WhereClause>(),
                    OrderByClauses = new List<OrderByClause>()
                }
            };

            //System.Diagnostics.Debugger.Break(); // breakpoint before HTTP call to GetClientBusinessRules

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetClientBusinessRules"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<GetClientBusinessRulesResponse>(content);
                var allRules = response?.ClientBusinessRules ?? new List<ClientBusinessRule>();

                return allRules.Where(r => profileCbrIds.Contains(r.Id)).ToList();
            }
        }

        /// <summary>
        /// Retrieves all <see cref="ServerBusinessRule"/> records for the current company
        /// and pairs each rule with the list of profile names that reference it.
        /// </summary>
        public List<(ServerBusinessRule Rule, List<string> ProfileNames)> GetServerBusinessRulesForCompany()
        {
            _logger.LogTrace(">> GetServerBusinessRulesForCompany | CompanyId={CompanyId}", _companyId);

            var profiles = GetProfiles();

            // Build SBR-ID → profile-names lookup
            var sbrProfileMap = profiles
                .Where(p => p.ServerBusinessRuleId != 0)
                .GroupBy(p => p.ServerBusinessRuleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Name ?? string.Empty)
                          .Where(n => !string.IsNullOrEmpty(n))
                          .ToList());

            var accessToken = GetAccessToken();
            var request = new GetServerBusinessRulesRequest
            {
                CompanyId = _companyId,
                SearchCriteria = new SearchCriteria
                {
                    WhereClauses  = new List<WhereClause>(),
                    OrderByClauses = new List<OrderByClause>()
                }
            };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetServerBusinessRules"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<GetServerBusinessRulesResponse>(content);
                var allSbrs = response?.ServerBusinessRule ?? new List<ServerBusinessRule>();

                _logger.LogTrace("<< GetServerBusinessRulesForCompany → {Count} rules", allSbrs.Count);
                return allSbrs
                    .Select(r => (r, sbrProfileMap.TryGetValue(r.Id, out var names) ? names : new List<string>()))
                    .ToList();
            }
        }

        /// <summary>
        /// Retrieves all <see cref="ClientBusinessRule"/> records for the current company
        /// and pairs each rule with the list of profile names that reference it via
        /// <see cref="Profile.ClientBusinessRuleId"/>.
        /// </summary>
        public List<(ClientBusinessRule Rule, List<string> ProfileNames)> GetClientBusinessRulesWithProfilesForCompany()
        {
            _logger.LogTrace(">> GetClientBusinessRulesWithProfilesForCompany | CompanyId={CompanyId}", _companyId);

            var profiles = GetProfiles();

            var cbrProfileMap = profiles
                .Where(p => p.ClientBusinessRuleId != 0)
                .GroupBy(p => p.ClientBusinessRuleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Name ?? string.Empty)
                          .Where(n => !string.IsNullOrEmpty(n))
                          .ToList());

            var accessToken = GetAccessToken();
            var request = new GetClientBusinessRulesRequest
            {
                CompanyId = _companyId,
                SearchCriteria = new SearchCriteria
                {
                    WhereClauses  = new List<WhereClause>(),
                    OrderByClauses = new List<OrderByClause>()
                }
            };

            using (var httpClient = new HttpClient())
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _adminUrl + "GetClientBusinessRules"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string json = JsonHelper.Serialize(request);
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();

                string content = httpResponse.Content.ReadAsStringAsync().Result;
                var response = JsonHelper.Deserialize<GetClientBusinessRulesResponse>(content);
                var allRules = response?.ClientBusinessRules ?? new List<ClientBusinessRule>();

                _logger.LogTrace("<< GetClientBusinessRulesWithProfilesForCompany → {Count} rules", allRules.Count);
                return allRules
                    .Select(r => (r, cbrProfileMap.TryGetValue(r.Id, out var names) ? names : new List<string>()))
                    .ToList();
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
