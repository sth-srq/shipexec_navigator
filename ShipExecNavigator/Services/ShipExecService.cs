using Newtonsoft.Json;
using System.Diagnostics;
using PSI.Sox.Wcf.Administration;
using ShipExecNavigator.BusinessLogic;
using ShipExecNavigator.BusinessLogic.CompanyBuilder;
using ShipExecNavigator.BusinessLogic.EntityComparison;
using ShipExecNavigator.BusinessLogic.RequestGeneration;
using ShipExecNavigator.DAL.Managers;
using ShipExecNavigator.Helpers;
using ShipExecNavigator.Shared.Helpers;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;
using CompanyInfo = ShipExecNavigator.Shared.Models.CompanyInfo;
using DalVariance = ShipExecNavigator.DAL.Entities.Variance;
using DalTemplate = ShipExecNavigator.DAL.Entities.CompanyTemplate;

namespace ShipExecNavigator.Services;

public sealed class ShipExecService(VarianceManager varianceManager, TemplateManager templateManager) : IShipExecService
{
    private AppManager? _appManager;
    private string?     _adminUrl;
    private Guid        _currentCompanyId;
    private List<Variance>? _lastVariances;
    private string? _lastModifiedXml;
    private string? _lastCompanyXml;

    public Task<List<CompanyInfo>> GetCompaniesAsync(string jwtJson, string adminUrl)
    {
        _appManager = new AppManager(jwtJson, adminUrl);
        _adminUrl   = adminUrl.EndsWith('/') ? adminUrl : adminUrl + "/";
        return Task.Run(() =>
        {
            var companies = _appManager.GetCompanies();
            return companies
                .Select(c => new CompanyInfo { Id = c.Id, Name = c.Name, Symbol = c.Symbol })
                .ToList();
        });
    }

    // ── Live connection (lazy tree) ──────────────────────────────────────────

    public Task SetupCompanyAsync(Guid companyId, string companyName)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        _currentCompanyId = companyId;
        _appManager.SetCompany(companyId, companyName);
        return Task.CompletedTask;
    }

    public Task<XmlNodeViewModel> BuildCompanySkeletonAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() =>
        {
            var company = _appManager.GetCompanyBase();

            var root = new XmlNodeViewModel
            {
                NodeName = "Company",
                Depth = 0,
                IsExpanded = true,
            };

            // Add scalar company properties as immediate leaf nodes
            AddScalarNode(root, "Id", company.Id.ToString());
            AddScalarNode(root, "Name", company.Name);
            AddScalarNode(root, "Symbol", company.Symbol);

            // Add lazy-loadable category nodes for entity collections
            var categories = new (string Name, string Key)[]
            {
                ("Shippers",                  "Shippers"),
                ("Clients",                   "Clients"),
                ("Profiles",                  "Profiles"),
                ("Sites",                     "Sites"),
                ("AdapterRegistrations",      "AdapterRegistrations"),
                ("CarrierRoutes",             "CarrierRoutes"),
                ("DataConfigurationMappings", "DataConfigurationMappings"),
                ("DocumentConfigurations",    "DocumentConfigurations"),
                ("Machines",                  "Machines"),
                ("PrinterConfigurations",     "PrinterConfigurations"),
                ("PrinterDefinitions",        "PrinterDefinitions"),
                ("ScaleConfigurations",       "ScaleConfigurations"),
                ("Schedules",                 "Schedules"),
                ("SourceConfigurations",      "SourceConfigurations"),
            };

            foreach (var (name, key) in categories)
            {
                root.Children.Add(EntityTreeBuilder.CreateLazyCategoryNode(name, key, 1, root));
            }

            return root;
        });
    }

    public Task LoadCategoryChildrenAsync(XmlNodeViewModel categoryNode)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        var key = categoryNode.LazyLoadKey;
        if (string.IsNullOrEmpty(key)) return Task.CompletedTask;

        return Task.Run(() =>
        {
            var jwt = _appManager.GetAccessToken();
            var url = _appManager.AdminUrl;
            var cid = _currentCompanyId;

            switch (key)
            {
                case "Shippers":
                    PopulateCategory(categoryNode, "Shipper",
                        _appManager.GetShippers());
                    break;

                case "Clients":
                    PopulateCategory(categoryNode, "Client",
                        _appManager.GetClients());
                    break;

                case "Profiles":
                    PopulateCategory(categoryNode, "Profile",
                        _appManager.GetProfiles());
                    break;

                case "Sites":
                    PopulateCategory(categoryNode, "Site",
                        _appManager.GetSites(cid));
                    break;

                case "AdapterRegistrations":
                    FetchAndPopulate(categoryNode, "AdapterRegistration",
                        new AdapterRegistrationFetcher(url, cid, jwt),
                        r => r?.AdapterRegistrations);
                    break;

                case "CarrierRoutes":
                    FetchAndPopulate(categoryNode, "CarrierRoute",
                        new CarrierRouteFetcher(url, cid, jwt),
                        r => r?.CarrierRoute);
                    break;

                case "DataConfigurationMappings":
                    FetchAndPopulate(categoryNode, "DataConfigurationMapping",
                        new DataConfigurationMappingFetcher(url, cid, jwt),
                        r => r?.DataConfigurations);
                    break;

                case "DocumentConfigurations":
                    FetchAndPopulate(categoryNode, "DocumentConfiguration",
                        new DocumentConfigurationFetcher(url, cid, jwt),
                        r => r?.DocumentConfigurations);
                    break;

                case "Machines":
                    FetchAndPopulate(categoryNode, "Machine",
                        new MachineFetcher(url, cid, jwt),
                        r => r?.Machines);
                    break;

                case "PrinterConfigurations":
                    FetchAndPopulate(categoryNode, "PrinterConfiguration",
                        new PrinterConfigurationFetcher(url, cid, jwt),
                        r => r?.PrinterConfigurations);
                    break;

                case "PrinterDefinitions":
                    FetchAndPopulate(categoryNode, "PrinterDefinition",
                        new PrinterDefinitionFetcher(url, cid, jwt),
                        r => r?.PrinterDefinitions);
                    break;

                case "ScaleConfigurations":
                    FetchAndPopulate(categoryNode, "ScaleConfiguration",
                        new ScaleConfigurationFetcher(url, cid, jwt),
                        r => r?.ScaleConfigurations);
                    break;

                case "Schedules":
                    FetchAndPopulate(categoryNode, "Schedule",
                        new ScheduleFetcher(url, cid, jwt),
                        r => r?.Schedules);
                    break;

                case "SourceConfigurations":
                    FetchAndPopulate(categoryNode, "SourceConfiguration",
                        new SourceConfigurationFetcher(url, cid, jwt),
                        r => r?.SourceConfigurations);
                    break;

                default:
                    categoryNode.IsLazyLoaded = true;
                    break;
            }
        });
    }

    private static void PopulateCategory<T>(XmlNodeViewModel parentNode, string itemName, List<T> items)
        where T : class
    {
        EntityTreeBuilder.PopulateCollectionNode(parentNode, itemName, items);
    }

    private static void FetchAndPopulate<TReq, TRes>(
        XmlNodeViewModel parentNode,
        string itemName,
        EntityFetcher<TReq, TRes> fetcher,
        Func<TRes, System.Collections.IEnumerable?> extractItems)
        where TReq : PSI.Sox.Wcf.RequestBase, new()
        where TRes : new()
    {
        try
        {
            var response = fetcher.Fetch();
            var items = extractItems(response);
            if (items is not null)
                EntityTreeBuilder.PopulateCollectionNode(parentNode, itemName, items);
            else
                parentNode.IsLazyLoaded = true;
        }
        catch
        {
            parentNode.IsLazyLoaded = true;
        }
    }

    private static void AddScalarNode(XmlNodeViewModel parent, string name, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        parent.Children.Add(new XmlNodeViewModel
        {
            NodeName = name,
            NodeValue = value,
            OriginalNodeValue = value,
            Depth = parent.Depth + 1,
            Parent = parent,
        });
    }

    // ── Diff / apply (deferred) ─────────────────────────────────────────────

    public Task<string> GetCompanyXmlAsync(Guid companyId, string companyName, string path = "", HashSet<string>? loadedSections = null)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        _appManager.SetCompany(companyId, companyName);
        return Task.Run(() =>
        {
            var companyXml = _appManager.GetCompanyXmlString(path, companyName, loadedSections);
            _lastCompanyXml = companyXml;
            return companyXml;
        });
    }

    public Task<DiffResult> GetDiffAsync(string originalXml, string modifiedXml)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() =>
        {
            var cleanOriginal = StripUsersNode(originalXml);
            var cleanModified = StripUsersNode(NormalizeForShipperDiff(originalXml, modifiedXml));

            var result = _appManager!.GetVariancesAndRequests(cleanOriginal, cleanModified);
            var variances = result.Item1;
            var requests  = result.Item2;

            _lastVariances   = variances;
            _lastModifiedXml = cleanModified;

            return BuildDiffResult(variances, requests);
        });
    }

    private static DiffResult BuildDiffResult(List<Variance> variances, List<RequestBaseWithURL> requests)
    {
        return new DiffResult
        {
            Variances = variances
                .Select((v, i) => (v, i))
                .SelectMany(t =>
                {
                    var (v, i) = t;
                    if (v.ChildVariances != null && v.ChildVariances.Count > 0)
                    {
                        return v.ChildVariances.Select(cv => new VarianceInfo
                        {
                            VarianceIndex = i,
                            EntityName    = cv.EntityName,
                            ChangeType    = cv.IsAdd ? "Add" : cv.IsRemove ? "Remove" : "Update",
                            ParentContext = cv.ParentContext,
                            OriginalXML  = cv.OriginalObject is not null
                                ? JsonConvert.SerializeObject(cv.OriginalObject, Formatting.Indented)
                                : string.Empty,
                            NewXML = cv.NewObject is not null
                                ? JsonConvert.SerializeObject(cv.NewObject, Formatting.Indented)
                                : string.Empty,
                        });
                    }
                    return new[]
                    {
                        new VarianceInfo
                        {
                            VarianceIndex = i,
                            EntityName   = v.EntityName,
                            ChangeType   = v.IsAdd ? "Add" : v.IsRemove ? "Remove" : "Update",
                            OriginalXML = v.OriginalObject is not null
                                ? JsonConvert.SerializeObject(v.OriginalObject, Formatting.Indented)
                                : string.Empty,
                            NewXML = v.NewObject is not null
                                ? JsonConvert.SerializeObject(v.NewObject, Formatting.Indented)
                                : string.Empty,
                        }
                    };
                })
                // Suppress phantom update variances — entities where only complex/nested fields
                // (e.g. Carriers) differ due to XML serialization noise; scalar fields are identical
                // so there is nothing displayable to show and nothing the user can act on here.
                .Where(vi => !IsPhantomUpdate(vi))
                .ToList(),

            Requests = requests.Select(r => new RequestInfo
            {
                EntityName  = r.EntityName,
                Operation   = r.IsAdd ? "Add" : r.IsDelete ? "Remove" : "Update",
                Endpoint    = r.Endpoint,
                RequestJson = r.Request is not null
                    ? JsonConvert.SerializeObject(r.Request, Formatting.Indented)
                    : string.Empty,
            }).ToList(),
        };
    }
    /// <summary>
    /// Returns true when an Update variance has no displayable scalar-field changes —
    /// i.e. the diff was triggered solely by serialization differences in complex
    /// properties (arrays, nested objects) that <see cref="FieldDiffHelper"/> skips.
    /// Such variances clutter the display with "No detail available" and are suppressed.
    /// The underlying API requests are still generated from <c>_lastVariances</c>.
    /// </summary>
    private static bool IsPhantomUpdate(VarianceInfo vi) =>
        vi.ChangeType == "Update"
        && !string.IsNullOrEmpty(vi.OriginalXML)
        && !string.IsNullOrEmpty(vi.NewXML)
        && FieldDiffHelper.GetChangedFields(vi.OriginalXML, vi.NewXML).Count == 0;

    private readonly List<PSI.Sox.User> _pendingUserUpdates = [];

    public void EnqueueUserUpdate(PSI.Sox.User user)
    {
        _pendingUserUpdates.RemoveAll(u => u.Id == user.Id);
        _pendingUserUpdates.Add(user);
    }

    public Task<List<ApplyResultItem>> ApplyChangesAsync(string? comments = null)
    {
        if (_lastVariances is null)
            throw new InvalidOperationException("No diff computed. Call GetDiffAsync first.");
        return ApplyChangesAsync(Enumerable.Range(0, _lastVariances.Count).ToList(), comments);
    }

    public async Task<List<ApplyResultItem>> ApplyChangesAsync(IReadOnlyList<int> selectedVarianceIndices, string? comments = null)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");
        if (_lastVariances is null || _lastModifiedXml is null)
            throw new InvalidOperationException("No diff computed. Call GetDiffAsync first.");

        var companyId = _appManager.GetCurrentCompanyId();
        var variances = selectedVarianceIndices
            .Where(i => i >= 0 && i < _lastVariances.Count)
            .Select(i => _lastVariances[i])
            .Distinct()
            .ToList();
        var modifiedXml = _lastModifiedXml;

        var results = await Task.Run(() =>
            _appManager.ApplyChanges(modifiedXml, variances)
                        .Select(r => new ApplyResultItem
                        {
                            EntityName = r.EntityName,
                            Operation  = r.Operation,
                            Endpoint   = r.Endpoint,
                            Success    = r.Success,
                            Message    = r.Message,
                        })
                        .ToList()
        );

        await LogVariancesAsync(variances, results, comments, companyId, _adminUrl);

        var pendingUsers = _pendingUserUpdates.ToList();
        _pendingUserUpdates.Clear();
        foreach (var user in pendingUsers)
        {
            try
            {
                await Task.Run(() => _appManager.UpdateUser(user));
                results.Add(new ApplyResultItem
                {
                    EntityName = user.UserName ?? user.Id.ToString(),
                    Operation  = "Update",
                    Endpoint   = "UpdateUser",
                    Success    = true,
                    Message    = "User updated successfully",
                });
            }
            catch (Exception ex)
            {
                results.Add(new ApplyResultItem
                {
                    EntityName = user.UserName ?? user.Id.ToString(),
                    Operation  = "Update",
                    Endpoint   = "UpdateUser",
                    Success    = false,
                    Message    = ex.Message,
                });
            }
        }

        return results;
    }

    public Task<List<PSI.Sox.User>> GetUsersAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.GetUsers());
    }

    public void PrepareForApply(Guid companyId, string companyName)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");
        _appManager.SetCompany(companyId, companyName);
    }

    private async Task LogVariancesAsync(List<Variance> variances, List<ApplyResultItem> results, string? comments, Guid companyId, string? endpoint)
    {
        try
        {
            var batchId   = Guid.NewGuid();
            var opts      = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

            foreach (var result in results.Where(r => r.Success))
            {
                var matchingVariance = variances.FirstOrDefault(v =>
                    string.Equals(v.EntityName, result.EntityName, StringComparison.OrdinalIgnoreCase));

                await varianceManager.InsertAsync(new DalVariance
                {
                    BatchId        = batchId,
                    CompanyId      = companyId,
                    UserId         = Guid.Empty,
                    Comments       = comments,
                    Endpoint       = endpoint,
                    NewEntity      = matchingVariance?.NewObject is not null
                        ? JsonConvert.SerializeObject(matchingVariance.NewObject, opts)
                        : null,
                    OriginalEntity = matchingVariance?.OriginalObject is not null
                        ? JsonConvert.SerializeObject(matchingVariance.OriginalObject, opts)
                        : null,
                    VarianceData   = matchingVariance is not null
                        ? JsonConvert.SerializeObject(matchingVariance, opts)
                        : null,
                });
            }
        }
        catch
        {
            // Logging failures must never interrupt the apply flow
        }
    }

    public async Task<List<Variance>> GetCompanyVarianceHistoryAsync()
    {
        if (_currentCompanyId == Guid.Empty)
            throw new InvalidOperationException("No company selected. Open a company before loading history.");
        var companyId = _currentCompanyId;

        List<DalVariance> dalVariances;
        try
        {
            dalVariances = (await varianceManager.GetByCompanyAsync(companyId)).ToList();
        }
        catch
        {
            // Database is unreachable — return empty history so the caller can continue normally.
            return [];
        }
        var opts = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        var entries = new List<Variance>();

        foreach (var dv in dalVariances)
        {
            var blVariance = dv.VarianceData is not null
                ? JsonConvert.DeserializeObject<Variance>(dv.VarianceData, opts)
                : null;

            if (blVariance?.ChildVariances is { Count: > 0 })
            {
                // Flatten children, same as BuildDiffResult
                foreach (var cv in blVariance.ChildVariances)
                {
                    var changeType = cv.IsAdd ? "Added" : cv.IsRemove ? "Removed" : "Modified";
                    entries.Add(new Variance
                    {
                        PathDescription = cv.EntityName ?? blVariance.EntityName ?? "Unknown",
                        Description     = string.IsNullOrWhiteSpace(dv.Comments)
                            ? $"{changeType} on {dv.CreatedOn:g}"
                            : dv.Comments,
                        ChangeType      = changeType,
                        OriginalXML    = cv.OriginalObject is not null
                            ? JsonConvert.SerializeObject(cv.OriginalObject, opts)
                            : string.Empty,
                        NewXML         = cv.NewObject is not null
                            ? JsonConvert.SerializeObject(cv.NewObject, opts)
                            : string.Empty,
                        IsHistorical    = true,
                        Timestamp       = dv.CreatedOn,
                        Comments        = dv.Comments,
                        VarianceJson    = dv.VarianceData,
                    });
                }
            }
            else
            {
                var changeType = blVariance is not null
                    ? (blVariance.IsAdd ? "Added" : blVariance.IsRemove ? "Removed" : "Modified")
                    : "Modified";
                entries.Add(new Variance
                {
                    PathDescription = blVariance?.EntityName ?? "Unknown",
                    Description     = string.IsNullOrWhiteSpace(dv.Comments)
                        ? $"{changeType} on {dv.CreatedOn:g}"
                        : dv.Comments,
                    ChangeType      = changeType,
                    OriginalXML    = dv.OriginalEntity ?? string.Empty,
                    NewXML         = dv.NewEntity ?? string.Empty,
                    IsHistorical    = true,
                    Timestamp       = dv.CreatedOn,
                    Comments        = dv.Comments,
                    VarianceJson    = dv.VarianceData,
                });
            }
        }

        return entries;
    }

    public Task<List<PSI.Sox.Profile>> GetProfilesAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.GetProfiles());
    }

    public Task<PSI.Sox.Profile> GetFullProfileAsync(int profileId)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.GetFullProfile(profileId));
    }

    public Task<List<PSI.Sox.Shipper>> GetShippersAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.GetShippers());
    }

    public Task<string> ExportShippersCsvAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() =>
        {
            var shippers    = _appManager.GetShippers();
            var sites       = _appManager.GetSites(_appManager.GetCurrentCompanyId());
            var clientLogic = ShipExecNavigator.ClientSpecificLogic.ClientLogicResolver.Resolve(GetCurrentCompany()?.Name);

            var siteNames = sites
                .Where(s => s.Id != Guid.Empty && !string.IsNullOrEmpty(s.Name))
                .ToDictionary(s => s.Id, s => s.Name ?? string.Empty);

            // Collect all unique CustomData keys across all shippers (preserve first-seen order)
            var customKeys = new List<string>();
            var customKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sh in shippers)
            {
                foreach (var cd in sh.CustomData ?? [])
                {
                    if (!string.IsNullOrEmpty(cd.Key) && customKeySet.Add(cd.Key))
                        customKeys.Add(cd.Key);
                }
            }

            var sb = new System.Text.StringBuilder();

            // Header — matches the field names used by ImportShippersDialog so the file can be re-imported
            var headerFields = new List<string>
            {
                "Name", "Symbol", "Code",
                "Address1", "Address2", "Address3", "City", "StateProvince", "PostalCode", "Country",
                "Company", "Contact", "Phone", "Fax", "Email", "Sms",
                "PoBox", "Residential",
                "SiteName",
            };
            foreach (var key in customKeys)
                headerFields.Add($"CustomData:{key}");
            foreach (var extraHeader in clientLogic.GetShipperExportExtraHeaders())
                headerFields.Add(extraHeader);

            sb.AppendLine(string.Join(",", headerFields.Select(EscapeCsv)));

            foreach (var sh in shippers)
            {
                siteNames.TryGetValue(sh.SiteId ?? Guid.Empty, out var siteName);

                var fields = new List<string>
                {
                    EscapeCsv(sh.Name          ?? string.Empty),
                    EscapeCsv(sh.Symbol        ?? string.Empty),
                    EscapeCsv(sh.Code          ?? string.Empty),
                    EscapeCsv(sh.Address1      ?? string.Empty),
                    EscapeCsv(sh.Address2      ?? string.Empty),
                    EscapeCsv(sh.Address3      ?? string.Empty),
                    EscapeCsv(sh.City          ?? string.Empty),
                    EscapeCsv(sh.StateProvince ?? string.Empty),
                    EscapeCsv(sh.PostalCode    ?? string.Empty),
                    EscapeCsv(sh.Country       ?? string.Empty),
                    EscapeCsv(sh.Company       ?? string.Empty),
                    EscapeCsv(sh.Contact       ?? string.Empty),
                    EscapeCsv(sh.Phone         ?? string.Empty),
                    EscapeCsv(sh.Fax           ?? string.Empty),
                    EscapeCsv(sh.Email         ?? string.Empty),
                    EscapeCsv(sh.Sms           ?? string.Empty),
                    EscapeCsv(sh.PoBox       ? "1" : "0"),
                    EscapeCsv(sh.Residential ? "1" : "0"),
                    EscapeCsv(siteName         ?? string.Empty),
                };

                var cdLookup = (sh.CustomData ?? [])
                    .Where(c => !string.IsNullOrEmpty(c.Key))
                    .ToDictionary(c => c.Key!, c => c.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                foreach (var key in customKeys)
                {
                    cdLookup.TryGetValue(key, out var cdVal);
                    fields.Add(EscapeCsv(cdVal ?? string.Empty));
                }

                foreach (var extraVal in clientLogic.GetShipperExportExtraValues(sh))
                    fields.Add(EscapeCsv(extraVal));

                sb.AppendLine(string.Join(",", fields));
            }

            return sb.ToString();
        });
    }

    public async Task<List<Variance>> GetShipperVariancesAsync(List<PSI.Sox.Shipper> incoming)
    {
        var existing = await GetShippersAsync();
        var result = new List<Variance>();

        var clientLogic = ShipExecNavigator.ClientSpecificLogic.ClientLogicResolver.Resolve(GetCurrentCompany()?.Name);

        // Build a lookup keyed by Symbol for O(n) matching
        var existingBySymbol = existing
            .Where(e => !string.IsNullOrEmpty(e.Symbol))
            .ToDictionary(e => e.Symbol!, StringComparer.OrdinalIgnoreCase);

        foreach (var inc in incoming)
        {
            if (string.IsNullOrWhiteSpace(inc.Symbol))
                continue; // Symbol is the unique key; rows without it cannot be matched or added

            existingBySymbol.TryGetValue(inc.Symbol, out var match);
            match ??= clientLogic.FindMatchingShipper(existing, inc);

            if (match is null)
            {
                result.Add(new Variance { EntityName = "Shipper", IsAdd = true, NewObject = inc });
            }
            else
            {
                var merged  = MergeShipper(match, inc);
                var changes = GetShipperChangedFields(match, merged);
                if (changes.Count > 0)
                    result.Add(new Variance { EntityName = "Shipper", IsUpdated = true, NewObject = merged, OriginalObject = match });
            }
        }

        return result;
    }

    public Task<List<ApplyResultItem>> ApplyShipperVariancesAsync(List<Variance> variances)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        if (_lastCompanyXml is null)
            throw new InvalidOperationException("No company XML cached. Call GetCompanyXmlAsync first.");

        var xml = _lastCompanyXml;
        return Task.Run(() =>
            _appManager.ApplyChanges(xml, variances)
                       .Select(r => new ApplyResultItem
                       {
                           EntityName = r.EntityName,
                           Operation  = r.Operation,
                           Endpoint   = r.Endpoint,
                           Success    = r.Success,
                           Message    = r.Message,
                       })
                       .ToList()
        );
    }

    /// <summary>
    /// Builds a shipper for an Update variance by starting from the existing shipper
    /// and overlaying only the fields that were actually provided (non-empty) in the
    /// incoming import row.  Fields absent from the import retain their existing values.
    /// </summary>
    private static PSI.Sox.Shipper MergeShipper(PSI.Sox.Shipper existing, PSI.Sox.Shipper incoming)
    {
        var siteId = incoming.SiteId.HasValue && incoming.SiteId.Value != Guid.Empty
                         ? incoming.SiteId
                         : existing.SiteId;

        //Debugger.Break(); // ← breakpoint: before SiteId is assigned to the merged Shipper

        return new PSI.Sox.Shipper
        {
            // Preserve system / identity fields from the existing record
            Id            = existing.Id,
            CompanyId     = existing.CompanyId,
            SiteId        = siteId,
            Symbol        = !string.IsNullOrEmpty(incoming.Symbol) ? incoming.Symbol : existing.Symbol,

            // String fields: use incoming value only when non-empty
            Name          = !string.IsNullOrEmpty(incoming.Name)          ? incoming.Name          : existing.Name,
            Code          = !string.IsNullOrEmpty(incoming.Code)          ? incoming.Code          : existing.Code,
            Address1      = !string.IsNullOrEmpty(incoming.Address1)      ? incoming.Address1      : existing.Address1,
            Address2      = !string.IsNullOrEmpty(incoming.Address2)      ? incoming.Address2      : existing.Address2,
            Address3      = !string.IsNullOrEmpty(incoming.Address3)      ? incoming.Address3      : existing.Address3,
            City          = !string.IsNullOrEmpty(incoming.City)          ? incoming.City          : existing.City,
            StateProvince = !string.IsNullOrEmpty(incoming.StateProvince) ? incoming.StateProvince : existing.StateProvince,
            PostalCode    = !string.IsNullOrEmpty(incoming.PostalCode)    ? incoming.PostalCode    : existing.PostalCode,
            Country       = !string.IsNullOrEmpty(incoming.Country)       ? incoming.Country       : existing.Country,
            Company       = !string.IsNullOrEmpty(incoming.Company)       ? incoming.Company       : existing.Company,
            Contact       = !string.IsNullOrEmpty(incoming.Contact)       ? incoming.Contact       : existing.Contact,
            Phone         = !string.IsNullOrEmpty(incoming.Phone)         ? incoming.Phone         : existing.Phone,
            Fax           = !string.IsNullOrEmpty(incoming.Fax)           ? incoming.Fax           : existing.Fax,
            Email         = !string.IsNullOrEmpty(incoming.Email)         ? incoming.Email         : existing.Email,
            Sms           = !string.IsNullOrEmpty(incoming.Sms)           ? incoming.Sms           : existing.Sms,

            // Boolean fields: incoming can only SET a flag to true; clearing is not supported via import
            PoBox         = incoming.PoBox         || existing.PoBox,
            Residential   = incoming.Residential   || existing.Residential,

            // Preserve existing custom data; merge in any incoming entries by key
            CustomData    = MergeCustomData(existing.CustomData, incoming.CustomData),
        };
    }

    /// <summary>
    /// Returns the existing custom-data list with any incoming entries overlaid by key.
    /// Keys present in <paramref name="incoming"/> are added or overwritten; all other
    /// existing keys are preserved.
    /// </summary>
    public int AppendPendingVariances(List<Variance> variances)
    {
        _lastVariances ??= [];
        var startIndex = _lastVariances.Count;
        _lastVariances.AddRange(variances);
        return startIndex;
    }

    private static List<PSI.Sox.CustomData> MergeCustomData(
        List<PSI.Sox.CustomData>? existing,
        List<PSI.Sox.CustomData>? incoming)
    {
        if (incoming is null || incoming.Count == 0) return existing ?? [];
        var merged = (existing ?? [])
            .ToDictionary(c => c.Key ?? string.Empty, c => c.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var item in incoming.Where(i => !string.IsNullOrEmpty(i.Key)))
            merged[item.Key!] = item.Value;
        return [..merged.Select(kv => new PSI.Sox.CustomData { Key = kv.Key, Value = kv.Value })];
    }

    private static List<string> GetShipperChangedFields(PSI.Sox.Shipper existing, PSI.Sox.Shipper incoming)
    {
        var changes = new List<string>();

        void CheckStr(string field, string? a, string? b)
        {
            if (!string.IsNullOrEmpty(b) && !string.Equals(a ?? string.Empty, b, StringComparison.Ordinal))
                changes.Add(field);
        }

        CheckStr("Symbol",        existing.Symbol,        incoming.Symbol);
        CheckStr("Name",          existing.Name,          incoming.Name);
        CheckStr("Code",          existing.Code,          incoming.Code);
        CheckStr("Address1",      existing.Address1,      incoming.Address1);
        CheckStr("Address2",      existing.Address2,      incoming.Address2);
        CheckStr("Address3",      existing.Address3,      incoming.Address3);
        CheckStr("City",          existing.City,          incoming.City);
        CheckStr("StateProvince", existing.StateProvince, incoming.StateProvince);
        CheckStr("PostalCode",    existing.PostalCode,    incoming.PostalCode);
        CheckStr("Country",       existing.Country,       incoming.Country);
        CheckStr("Company",       existing.Company,       incoming.Company);
        CheckStr("Contact",       existing.Contact,       incoming.Contact);
        CheckStr("Phone",         existing.Phone,         incoming.Phone);
        CheckStr("Fax",           existing.Fax,           incoming.Fax);
        CheckStr("Email",         existing.Email,         incoming.Email);
        CheckStr("Sms",           existing.Sms,           incoming.Sms);
        if (incoming.PoBox       && existing.PoBox       != incoming.PoBox)       changes.Add("PoBox");
        if (incoming.Residential && existing.Residential != incoming.Residential) changes.Add("Residential");
        if (incoming.SiteId.HasValue && incoming.SiteId.Value != Guid.Empty && incoming.SiteId != existing.SiteId)
            changes.Add("SiteId");
        if (incoming.CustomData is { Count: > 0 })
        {
            var existingCd = (existing.CustomData ?? [])
                .ToDictionary(c => c.Key ?? string.Empty, c => c.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            foreach (var item in incoming.CustomData.Where(c => !string.IsNullOrEmpty(c.Key)))
            {
                existingCd.TryGetValue(item.Key!, out var existingVal);
                if (!string.Equals(existingVal ?? string.Empty, item.Value ?? string.Empty, StringComparison.Ordinal))
                    changes.Add($"CustomData[{item.Key}]");
            }
        }

        return changes;
    }

    public Task<PSI.Sox.User?> GetUserDetailAsync(Guid userId)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => (PSI.Sox.User?)_appManager.GetUserDetail(userId));
    }

    public Task<List<PSI.Sox.Permission>> GetPermissionsAsync(Guid userId)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.GetPermissions(userId));
    }

    public Task UpdateUserPermissionsAsync(PSI.Sox.User user, List<PSI.Sox.Permission> permissions)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.UpdateUserPermissions(user, permissions));
    }

    public Task<List<PSI.Sox.Role>> GetRolesAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.GetRoles());
    }

    public Task UpdateUserRolesAsync(PSI.Sox.User user, List<PSI.Sox.Role> roles)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.UpdateUserRoles(user, roles));
    }

    public Task UpdateUserAsync(PSI.Sox.User user)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.UpdateUser(user));
    }

    public Task<Guid> CreateUserAsync(PSI.Sox.User user)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.CreateUser(user));
    }

    public Task<List<CsvUserRow>> ParseCsvAsync(string csvContent)
    {
        return Task.FromResult(CsvUserParser.Parse(csvContent));
    }

    public Task<List<CsvUserCreateResult>> CreateUsersFromCsvAsync(List<CsvUserRow> rows)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() =>
        {
            var allRoles = _appManager.GetRoles();
            var allPerms = _appManager.GetPermissions(Guid.Empty);
            var results = new List<CsvUserCreateResult>();

            foreach (var row in rows.Where(r => r.IsValid))
            {
                var result = new CsvUserCreateResult { RowNumber = row.RowNumber, Email = row.Email };
                try
                {
                    var matchedRoles = allRoles
                        .Where(r => r.Name.ToLowerInvariant() == "user")
                        .ToList();

                    var requestedPermNames = _canViewFlags
                        .Where(f => f.Get(row))
                        .Select(f => f.Name)
                        .ToList();

                    var matchedPerms = allPerms
                        .Where(p => requestedPermNames.Any(n => string.Equals(n, p.Name, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var user = new PSI.Sox.User
                    {
                        UserName = row.Email.Trim(),
                        Email = row.Email.Trim(),
                        SiteId = Guid.TryParse(row.Campus, out var siteGuid) ? siteGuid : (Guid?)null,
                        Roles = matchedRoles,
                        Permissions = matchedPerms,
                        Address = BuildAddress(row),
                        DefaultConfiguration = BuildDefaultConfig(row),
                    };

                    var newId = _appManager.CreateUser(user);
                    result.Success = true;
                    result.UserId = newId;
                    result.Message = "Created successfully.";
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = ex.Message;
                }

                results.Add(result);
            }

            return results;
        });
    }

    private static readonly (string Name, Func<CsvUserRow, bool> Get)[] _canViewFlags =
    [
        ("CanViewAddressBook",       r => r.CanViewAddressBook),
        ("CanViewBatchManager",      r => r.CanViewBatchManager),
        ("CanViewCloseout",          r => r.CanViewCloseout),
        ("CanViewCreateBatch",       r => r.CanViewCreateBatch),
        ("CanViewDistributionList",  r => r.CanViewDistributionList),
        ("CanViewGroupManager",      r => r.CanViewGroupManager),
        ("CanViewHistory",           r => r.CanViewHistory),
        ("CanViewManageData",        r => r.CanViewManageData),
        ("CanViewManifestDocuments", r => r.CanViewManifestDocuments),
        ("CanViewPickupRequest",     r => r.CanViewPickupRequest),
        ("CanViewScanAndShip",       r => r.CanViewScanAndShip),
        ("CanViewShippingAndRating", r => r.CanViewShippingAndRating),
        ("CanViewTransmit",          r => r.CanViewTransmit),
    ];

    public Task<string> ExportUsersCsvAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() =>
        {
            var users = _appManager.GetUsers();
            var profiles = _appManager.GetProfiles();
            var profileLookup = profiles.ToDictionary(p => p.Id, p => p.Name ?? string.Empty);
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(string.Join(",", CsvUserParser.ExpectedHeaders));

            foreach (var user in users)
            {
                var detail = _appManager.GetUserDetail(user.Id) ?? user;
                var addr = detail.Address;
                var config = detail.DefaultConfiguration;

                var permNames = new HashSet<string>(
                    (detail.Permissions ?? []).Select(p => p.Name ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase);

                var contact = addr?.Contact ?? string.Empty;
                var spaceIdx = contact.IndexOf(' ');
                var fName = spaceIdx > 0 ? contact[..spaceIdx] : contact;
                var lName = spaceIdx > 0 ? contact[(spaceIdx + 1)..] : string.Empty;

                var profileName = detail.ProfileId.HasValue && profileLookup.TryGetValue(detail.ProfileId.Value, out var pn)
                    ? pn
                    : string.Empty;

                var fields = new List<string>
                {
                    EscapeCsv(detail.Id.ToString()),
                    EscapeCsv(detail.SiteId?.ToString() ?? string.Empty),
                    EscapeCsv(detail.SiteName ?? string.Empty),
                    EscapeCsv(addr?.Company ?? string.Empty),
                    EscapeCsv(fName),
                    EscapeCsv(lName),
                    EscapeCsv(addr?.Address1 ?? string.Empty),
                    EscapeCsv(addr?.Address2 ?? string.Empty),
                    EscapeCsv(addr?.Address3 ?? string.Empty),
                    EscapeCsv(addr?.City ?? string.Empty),
                    EscapeCsv(addr?.StateProvince ?? string.Empty),
                    EscapeCsv(addr?.PostalCode ?? string.Empty),
                    EscapeCsv(addr?.Country ?? string.Empty),
                    EscapeCsv(detail.Email ?? string.Empty),
                    EscapeCsv(addr?.Phone ?? string.Empty),
                    string.Empty, // IdLevel
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, // Custom1-5
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, // Custom6-10
                };

                foreach (var (name, _) in _canViewFlags)
                    fields.Add(permNames.Contains(name) ? "1" : "0");

                fields.Add(EscapeCsv(config?.ExportFileDelimiter.ToString() ?? string.Empty));
                fields.Add(EscapeCsv(config?.ExportFileQualifier.ToString() ?? string.Empty));
                fields.Add(EscapeCsv(config?.ExportFileGroupSeparator.ToString() ?? string.Empty));
                fields.Add(EscapeCsv(config?.ExportFileDecimalSeparator.ToString() ?? string.Empty));
                fields.Add(EscapeCsv(profileName));

                sb.AppendLine(string.Join(",", fields));
            }

            return sb.ToString();
        });
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static PSI.Sox.NameAddress BuildAddress(CsvUserRow row) => new()
    {
        Company = row.Company,
        Contact = $"{row.FName} {row.LName}".Trim(),
        Address1 = row.Addr1,
        Address2 = row.Addr2,
        Address3 = row.Addr3,
        City = row.City,
        StateProvince = row.State,
        PostalCode = row.Zip,
        Country = row.Country,
        Phone = row.Phone,
    };

    private static PSI.Sox.DefaultConfiguration BuildDefaultConfig(CsvUserRow row) => new()
    {
        ExportFileDelimiter = TryParseEnum(row.ExportFileDelimiter, PSI.Sox.Delimiter.Comma),
        ExportFileQualifier = TryParseEnum(row.ExportFileQualifier, PSI.Sox.Qualifier.DoubleQuotes),
        ExportFileGroupSeparator = TryParseEnum(row.ExportFileGroupSeparator, PSI.Sox.GroupSeparator.Comma),
        ExportFileDecimalSeparator = TryParseEnum(row.ExportFileDecimalSeparator, PSI.Sox.DecimalSeparator.Period),
    };

    private static T TryParseEnum<T>(string value, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : fallback;

    public Task<List<PSI.Sox.Site>> GetSitesAsync()
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() => _appManager.GetSites(_appManager.GetCurrentCompanyId()));
    }

    public Task<List<TemplateInfo>> GetCompanyTemplatesAsync(Guid companyId, string jwtJson, string adminUrl)
    {
        var tempManager = new ShipExecNavigator.BusinessLogic.AppManager(jwtJson, adminUrl);
        return Task.Run(() =>
            tempManager.GetTemplatesForCompany(companyId)
                       .Select(t => new TemplateInfo
                       {
                           Id           = t.Id,
                           TemplateName = t.TemplateName  ?? string.Empty,
                           TemplateType = t.TemplateType.ToString(),
                           TemplateData = t.TemplateHtml,
                       })
                       .ToList()
        );
    }

    public Task StoreCompanyTemplatesAsync(Guid companyId, List<TemplateInfo> templates, string endpointUrl, string companyName)
    {
        var rows = templates.Select(t => new DalTemplate
        {
            CompanyId    = companyId,
            TemplateId   = t.Id,
            CompanyName  = companyName,
            TemplateName = t.TemplateName,
            TemplateType = t.TemplateType,
            TemplateData = t.TemplateData,
            EndpointUrl  = endpointUrl,
        });

        return templateManager.UpsertBatchAsync(rows);
    }

    public Task<bool> CompanyHasStoredTemplatesAsync(Guid companyId)
        => templateManager.HasTemplatesAsync(companyId);

    public async Task<List<TemplateSaveResult>> SaveTemplatesToFolderAsync(string folderPath)
    {
        var templates = (await templateManager.GetAllAsync()).ToList();
        var results   = new List<TemplateSaveResult>();

        Directory.CreateDirectory(folderPath);

        foreach (var t in templates)
        {
            var randomSuffix = Guid.NewGuid().ToString("N")[..6];
            var baseName = SanitizeFileName(
                $"{t.CompanyName}_{t.TemplateType}_{t.TemplateName}_{randomSuffix}");

            var fileName = baseName + ".html";
            var filePath = Path.Combine(folderPath, fileName);

            var result = new TemplateSaveResult
            {
                FileName     = fileName,
                CompanyId    = t.CompanyId.ToString(),
                TemplateName = t.TemplateName,
                TemplateType = t.TemplateType,
            };

            try
            {
                await File.WriteAllTextAsync(filePath, t.TemplateData ?? string.Empty);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error   = ex.Message;
            }

            results.Add(result);
        }

        return results;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }

    public Task<(int Total, List<LogEntry> Logs)> GetApplicationLogsAsync(DateTime startDate, DateTime endDate)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() =>
        {
            var json = _appManager.GetApplicationLogsJson(startDate, endDate);
            var response = JsonConvert.DeserializeObject<LogsApiResponse>(json);
            return (response?.TotalRecords ?? 0, response?.Logs ?? new List<LogEntry>());
        });
    }

    private sealed class LogsApiResponse
    {
        [JsonProperty("TotalRecords")]
        public int TotalRecords { get; set; }

        [JsonProperty("Logs")]
        public List<LogEntry>? Logs { get; set; }
    }

    public Task<(int Total, List<SecurityLogEntry> Logs)> GetSecurityLogsAsync(DateTime startDate, DateTime endDate)
    {
        if (_appManager is null)
            throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");

        return Task.Run(() =>
        {
            var json = _appManager.GetSecurityLogsJson(startDate, endDate);
            var response = JsonConvert.DeserializeObject<SecurityLogsApiResponse>(json);
            return (response?.TotalRecords ?? 0, response?.Logs ?? new List<SecurityLogEntry>());
        });
    }

    private sealed class SecurityLogsApiResponse
    {
        [JsonProperty("TotalRecords")]
        public int TotalRecords { get; set; }

        [JsonProperty("Logs")]
        public List<SecurityLogEntry>? Logs { get; set; }
    }

    public CompanyInfo? GetCurrentCompany()
    {
        if (_appManager is null) return null;
        var id = _appManager.GetCurrentCompanyId();
        if (id == Guid.Empty) return null;
        return new CompanyInfo
        {
            Id = id,
            Name = _appManager.GetCurrentCompanyName(),
        };
    }

    private static string SerializeUsersXml(IEnumerable<PSI.Sox.User> users)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(
            typeof(PSI.Sox.User[]),
            new System.Xml.Serialization.XmlRootAttribute("Users"));
        var ns = new System.Xml.Serialization.XmlSerializerNamespaces();
        ns.Add("", "");
        var sb = new System.Text.StringBuilder();
        using var writer = new System.IO.StringWriter(sb);
        serializer.Serialize(writer, users.ToArray(), ns);
        return sb.ToString();
    }

    private static string InjectUsersIntoXml(string companyXml, string usersXml)
    {
        var doc = System.Xml.Linq.XDocument.Parse(companyXml);
        var usersDoc = System.Xml.Linq.XDocument.Parse(usersXml);
        doc.Root!.Add(usersDoc.Root);
        return doc.ToString();
    }

    private static string StripUsersNode(string xml)
    {
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            doc.Root?.Element("Users")?.Remove();
            return doc.ToString();
        }
        catch
        {
            return xml;
        }
    }

    /// <summary>
    /// Ensures that only Company-level shipper nodes (direct children of the root
    /// &lt;Shippers&gt; element) can differ between original and modified.
    /// Any &lt;Shippers&gt; containers nested deeper (e.g. inside Profiles or Sites)
    /// are restored to their original content so the diff engine never generates
    /// variances for them.
    /// </summary>
    private static string NormalizeForShipperDiff(string originalXml, string modifiedXml)
    {
        try
        {
            var origDoc = System.Xml.Linq.XDocument.Parse(originalXml);
            var modDoc  = System.Xml.Linq.XDocument.Parse(modifiedXml);

            var modRoot  = modDoc.Root;
            var origRoot = origDoc.Root;
            if (modRoot is null || origRoot is null) return modifiedXml;

            // Company-level Shippers = the <Shippers> element that is a direct child of root
            var modCompanyShippers  = modRoot.Element("Shippers");
            var origCompanyShippers = origRoot.Element("Shippers");

            // Every <Shippers> container nested deeper (Profile, Site, etc.)
            var modDeep  = modDoc.Descendants("Shippers")
                .Where(e => e != modCompanyShippers)
                .ToList();
            var origDeep = origDoc.Descendants("Shippers")
                .Where(e => e != origCompanyShippers)
                .ToList();

            int count = Math.Min(modDeep.Count, origDeep.Count);
            for (int i = 0; i < count; i++)
                modDeep[i].ReplaceWith(new System.Xml.Linq.XElement(origDeep[i]));

            // If modified somehow has extra containers, clear their Shipper children
            for (int i = count; i < modDeep.Count; i++)
                foreach (var s in modDeep[i].Elements("Shipper").ToList())
                    s.Remove();

            return modDoc.ToString();
        }
        catch
        {
            return modifiedXml;
        }
    }
}
