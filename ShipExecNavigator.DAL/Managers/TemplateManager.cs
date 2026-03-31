using Dapper;
using ShipExecNavigator.DAL.Entities;

namespace ShipExecNavigator.DAL.Managers;

public class TemplateManager(IDbConnectionFactory connectionFactory)
{
    public async Task<CompanyTemplate?> GetByIdAsync(long id)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<CompanyTemplate>(
            "SELECT * FROM dbo.CompanyTemplates WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<IEnumerable<CompanyTemplate>> GetByCompanyAsync(Guid companyId)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<CompanyTemplate>(
            """
            SELECT * FROM dbo.CompanyTemplates
            WHERE  CompanyId = @CompanyId
            ORDER  BY TemplateName
            """,
            new { CompanyId = companyId });
    }

    public async Task<bool> HasTemplatesAsync(Guid companyId)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.CompanyTemplates WHERE CompanyId = @CompanyId",
            new { CompanyId = companyId }) > 0;
    }

    public async Task<IEnumerable<CompanyTemplate>> GetAllAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<CompanyTemplate>(
            "SELECT * FROM dbo.CompanyTemplates ORDER BY CompanyId, TemplateName");
    }

    /// <summary>
    /// Inserts or updates a single template row keyed on (CompanyId, TemplateId).
    /// </summary>
    public async Task<long> UpsertAsync(CompanyTemplate template)
    {
        template.FetchedOn = DateTime.UtcNow;

        using var conn = connectionFactory.CreateConnection();
        template.Id = await conn.QuerySingleAsync<long>(
            """
            MERGE dbo.CompanyTemplates AS target
            USING (SELECT @CompanyId  AS CompanyId,
                          @TemplateId AS TemplateId) AS source
               ON target.CompanyId  = source.CompanyId
              AND target.TemplateId = source.TemplateId
            WHEN MATCHED THEN
                UPDATE SET CompanyName  = @CompanyName,
                           TemplateName = @TemplateName,
                           TemplateType = @TemplateType,
                           TemplateData = @TemplateData,
                           EndpointUrl  = @EndpointUrl,
                           FetchedOn    = @FetchedOn
            WHEN NOT MATCHED THEN
                INSERT (CompanyId, TemplateId, CompanyName, TemplateName, TemplateType,
                        TemplateData, EndpointUrl, FetchedOn)
                VALUES (@CompanyId, @TemplateId, @CompanyName, @TemplateName, @TemplateType,
                        @TemplateData, @EndpointUrl, @FetchedOn)
            OUTPUT inserted.Id;
            """,
            template);

        return template.Id;
    }

    /// <summary>
    /// Upserts a batch of templates for one company inside a single transaction.
    /// </summary>
    public async Task UpsertBatchAsync(IEnumerable<CompanyTemplate> templates)
    {
        var now = DateTime.UtcNow;

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var template in templates)
        {
            template.FetchedOn = now;
            await conn.ExecuteAsync(
                """
                MERGE dbo.CompanyTemplates AS target
                USING (SELECT @CompanyId  AS CompanyId,
                              @TemplateId AS TemplateId) AS source
                   ON target.CompanyId  = source.CompanyId
                  AND target.TemplateId = source.TemplateId
                WHEN MATCHED THEN
                    UPDATE SET CompanyName  = @CompanyName,
                               TemplateName = @TemplateName,
                               TemplateType = @TemplateType,
                               TemplateData = @TemplateData,
                               EndpointUrl  = @EndpointUrl,
                               FetchedOn    = @FetchedOn
                WHEN NOT MATCHED THEN
                    INSERT (CompanyId, TemplateId, CompanyName, TemplateName, TemplateType,
                            TemplateData, EndpointUrl, FetchedOn)
                    VALUES (@CompanyId, @TemplateId, @CompanyName, @TemplateName, @TemplateType,
                            @TemplateData, @EndpointUrl, @FetchedOn);
                """,
                template,
                transaction: tx);
        }

        tx.Commit();
    }

    public async Task DeleteByCompanyAsync(Guid companyId)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM dbo.CompanyTemplates WHERE CompanyId = @CompanyId",
            new { CompanyId = companyId });
    }

    public async Task DeleteAsync(long id)
    {
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM dbo.CompanyTemplates WHERE Id = @Id",
            new { Id = id });
    }
}
