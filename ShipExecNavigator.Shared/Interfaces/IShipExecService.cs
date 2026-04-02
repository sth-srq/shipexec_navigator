using PSI.Sox;
using ShipExecNavigator.BusinessLogic.EntityComparison;
using ShipExecNavigator.Shared.Models;
using CompanyInfo = ShipExecNavigator.Shared.Models.CompanyInfo;
using ShipExecNavigator.Shared.Helpers;

namespace ShipExecNavigator.Shared.Interfaces;

public interface IShipExecService
{
    Task<List<CompanyInfo>> GetCompaniesAsync(string jwtJson, string adminUrl);

    // ── Live connection (lazy tree) ──────────────────────────────────────────
    Task SetupCompanyAsync(Guid companyId, string companyName);
    Task<XmlNodeViewModel> BuildCompanySkeletonAsync();
    Task LoadCategoryChildrenAsync(XmlNodeViewModel categoryNode);

    // ── Diff / apply (deferred to "View Changes" time) ──────────────────────
    Task<string> GetCompanyXmlAsync(Guid companyId, string companyName, string path = "", HashSet<string>? loadedSections = null);
    Task<DiffResult> GetDiffAsync(string originalXml, string modifiedXml);
    Task<List<ApplyResultItem>> ApplyChangesAsync(string? comments = null);
    Task<List<ApplyResultItem>> ApplyChangesAsync(IReadOnlyList<int> selectedVarianceIndices, string? comments = null);

    // ── Users ────────────────────────────────────────────────────────────────
    Task<List<User>> GetUsersAsync();
    Task<User?> GetUserDetailAsync(Guid userId);
    Task<List<Permission>> GetPermissionsAsync(Guid userId);
    Task UpdateUserPermissionsAsync(User user, List<Permission> permissions);
    Task<List<Role>> GetRolesAsync();
    Task UpdateUserRolesAsync(User user, List<Role> roles);
    Task UpdateUserAsync(User user);
    void EnqueueUserUpdate(User user);
    Task<Guid> CreateUserAsync(User user);
    Task<List<CsvUserRow>> ParseCsvAsync(string csvContent);
    Task<List<CsvUserCreateResult>> CreateUsersFromCsvAsync(List<CsvUserRow> rows);
    Task<string> ExportUsersCsvAsync();
    Task<List<PSI.Sox.Site>> GetSitesAsync();

    // ── Logs ─────────────────────────────────────────────────────────────────
    Task<(int Total, List<LogEntry> Logs)> GetApplicationLogsAsync(DateTime startDate, DateTime endDate);
    Task<(int Total, List<SecurityLogEntry> Logs)> GetSecurityLogsAsync(DateTime startDate, DateTime endDate);

    // ── Company state ────────────────────────────────────────────────────────
    CompanyInfo? GetCurrentCompany();
    void PrepareForApply(Guid companyId, string companyName);

    // ── Profiles ─────────────────────────────────────────────────────────────
    Task<List<PSI.Sox.Profile>> GetProfilesAsync();
    Task<PSI.Sox.Profile> GetFullProfileAsync(int profileId);

    // ── Shippers ─────────────────────────────────────────────────────────────
    Task<List<PSI.Sox.Shipper>> GetShippersAsync();
    Task<string> ExportShippersCsvAsync();
    Task<List<Variance>> GetShipperVariancesAsync(List<PSI.Sox.Shipper> incoming);
    Task<List<ApplyResultItem>> ApplyShipperVariancesAsync(List<Variance> variances);
    /// <summary>
    /// Appends <paramref name="variances"/> to <c>_lastVariances</c> and returns the
    /// index of the first appended entry, so callers can assign correct VarianceIndex
    /// values to VarianceInfo entries for the diff dialog.
    /// </summary>
    int AppendPendingVariances(List<Variance> variances);

    // ── History / Templates ──────────────────────────────────────────────────
    Task<List<Variance>> GetCompanyVarianceHistoryAsync();
    Task<List<TemplateInfo>> GetCompanyTemplatesAsync(Guid companyId, string jwtJson, string adminUrl);
    Task StoreCompanyTemplatesAsync(Guid companyId, List<TemplateInfo> templates, string endpointUrl, string companyName);
    Task<bool> CompanyHasStoredTemplatesAsync(Guid companyId);
    Task<List<TemplateSaveResult>> SaveTemplatesToFolderAsync(string folderPath);
}
