using Dapper;
using Microsoft.Extensions.Logging;
using ShipExecAgent.DAL.Entities;

namespace ShipExecAgent.DAL.Managers;

/// <summary>
/// Data-access manager for the <c>dbo.Variances</c> table — the audit log that records
/// every entity change applied through ShipExec Navigator.
/// <para>
/// Each row in <c>dbo.Variances</c> corresponds to one entity change within a single
/// "apply" operation.  Related changes are grouped by a shared <c>BatchId</c> GUID so
/// the full set of changes from one "Apply" button click can be queried together.
/// </para>
/// <para>
/// <b>Key columns:</b>
/// <list type="table">
///   <listheader><term>Column</term><description>Purpose</description></listheader>
///   <item><term>BatchId</term><description>Groups all changes from a single apply operation.</description></item>
///   <item><term>CompanyId</term><description>Target company GUID.</description></item>
///   <item><term>OriginalEntity / NewEntity</term><description>JSON snapshots for before/after comparison.</description></item>
///   <item><term>VarianceData</term><description>Full serialised <c>Variance</c> object for deep inspection.</description></item>
///   <item><term>IsActive</term><description>Soft-delete flag; <see cref="DeactivateAsync"/> sets it to 0.</description></item>
/// </list>
/// </para>
/// <para>
/// All queries use Dapper with parameterised SQL to prevent injection.
/// </para>
/// </summary>
public class VarianceManager(IDbConnectionFactory connectionFactory, ILogger<VarianceManager> logger)
{
    public async Task<Variance?> GetByIdAsync(long id)
    {
        logger.LogTrace(">> GetByIdAsync({Id})", id);
        using var conn = connectionFactory.CreateConnection();
        var result = await conn.QuerySingleOrDefaultAsync<Variance>(
            "SELECT * FROM dbo.Variances WHERE Id = @Id",
            new { Id = id });
        logger.LogTrace("<< GetByIdAsync → {Found}", result is not null ? "found" : "null");
        return result;
    }

    public async Task<IEnumerable<Variance>> GetAllAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<Variance>(
            "SELECT * FROM dbo.Variances ORDER BY CreatedOn DESC");
    }

    public async Task<IEnumerable<Variance>> GetByCompanyAsync(Guid companyId)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<Variance>(
            "SELECT * FROM dbo.Variances WHERE CompanyId = @CompanyId ORDER BY CreatedOn DESC",
            new { CompanyId = companyId });
    }

    public async Task<IEnumerable<Variance>> GetActiveByCompanyAsync(Guid companyId)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<Variance>(
            "SELECT * FROM dbo.Variances WHERE CompanyId = @CompanyId AND IsActive = 1 ORDER BY CreatedOn DESC",
            new { CompanyId = companyId });
    }

    public async Task<long> InsertAsync(Variance variance)
    {
        logger.LogTrace(">> InsertAsync | BatchId={BatchId} CompanyId={CompanyId}",
            variance.BatchId, variance.CompanyId);
        variance.CreatedOn = DateTime.UtcNow;
        variance.IsActive  = true;

        using var conn = connectionFactory.CreateConnection();
        variance.Id = await conn.QuerySingleAsync<long>(
            """
            INSERT INTO dbo.Variances
                   (BatchId, CompanyId, UserId, NewEntity, OriginalEntity, VarianceData, Comments, Endpoint, CreatedOn, IsActive)
            VALUES (@BatchId, @CompanyId, @UserId, @NewEntity, @OriginalEntity, @VarianceData, @Comments, @Endpoint, @CreatedOn, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """,
            variance);

        return variance.Id;
    }

    public async Task UpdateAsync(Variance variance)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            """
            UPDATE dbo.Variances
            SET    BatchId        = @BatchId,
                   CompanyId      = @CompanyId,
                   UserId         = @UserId,
                   NewEntity      = @NewEntity,
                   OriginalEntity = @OriginalEntity,
                   VarianceData   = @VarianceData,
                   Comments       = @Comments,
                   Endpoint       = @Endpoint,
                   IsActive       = @IsActive
            WHERE  Id = @Id
            """,
            variance);
    }

    public async Task DeactivateAsync(long id)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE dbo.Variances SET IsActive = 0 WHERE Id = @Id",
            new { Id = id });
    }

    public async Task DeleteAsync(long id)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM dbo.Variances WHERE Id = @Id",
            new { Id = id });
    }
}
