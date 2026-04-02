using Dapper;
using Microsoft.Extensions.Logging;
using ShipExecNavigator.DAL.Entities;

namespace ShipExecNavigator.DAL.Managers;

/// <summary>
/// Data-access manager for the <c>dbo.ApiLogs</c> table — a structured log of every
/// outbound API call made by ShipExec Navigator to the Management Studio.
/// <para>
/// <b>Key columns:</b>
/// <list type="table">
///   <listheader><term>Column</term><description>Purpose</description></listheader>
///   <item><term>Category</term><description>High-level grouping (e.g. "Company", "User", "Shipper").</description></item>
///   <item><term>Operation</term><description>The specific API operation (e.g. "GetCompany", "UpdateShipper").</description></item>
///   <item><term>RequestData / ResponseData</term><description>JSON payloads for debugging.</description></item>
///   <item><term>DurationMs</term><description>End-to-end round-trip time in milliseconds.</description></item>
///   <item><term>IsSuccess</term><description>Whether the API call returned a 2xx status.</description></item>
///   <item><term>ErrorMessage</term><description>Set when <c>IsSuccess</c> is false.</description></item>
///   <item><term>CompanyId</term><description>Company in scope at the time of the call (nullable).</description></item>
/// </list>
/// </para>
/// </summary>
public class ApiLogManager(IDbConnectionFactory connectionFactory, ILogger<ApiLogManager> logger)
{
    public async Task<long> InsertAsync(ApiLog entry)
    {
        logger.LogTrace(">> InsertAsync | Category={Category} Operation={Operation}",
            entry.Category, entry.Operation);
        entry.OccurredOn = DateTime.UtcNow;

        using var conn = connectionFactory.CreateConnection();
        entry.Id = await conn.QuerySingleAsync<long>(
            """
            INSERT INTO dbo.ApiLogs
                   (OccurredOn, Category, Operation, RequestData, ResponseData,
                    DurationMs, IsSuccess, ErrorMessage, CompanyId, AdditionalInfo)
            VALUES (@OccurredOn, @Category, @Operation, @RequestData, @ResponseData,
                    @DurationMs, @IsSuccess, @ErrorMessage, @CompanyId, @AdditionalInfo);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """,
            entry);

        return entry.Id;
    }

    public async Task<IEnumerable<ApiLog>> GetRecentAsync(int top = 200)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<ApiLog>(
            "SELECT TOP (@Top) * FROM dbo.ApiLogs ORDER BY OccurredOn DESC",
            new { Top = top });
    }

    public async Task<IEnumerable<ApiLog>> GetByCompanyAsync(Guid companyId, int top = 200)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<ApiLog>(
            """
            SELECT TOP (@Top) *
            FROM   dbo.ApiLogs
            WHERE  CompanyId = @CompanyId
            ORDER  BY OccurredOn DESC
            """,
            new { Top = top, CompanyId = companyId });
    }
}
