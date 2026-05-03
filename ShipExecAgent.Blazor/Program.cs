using PSI.Sox;
using PSI.Sox.Configuration;
using QuestPDF.Infrastructure;
using Serilog;
using ShipExecAgent.AppLogic;
using ShipExecAgent.Blazor.Components;
using ShipExecAgent.BusinessLogic.Logging;
using ShipExecAgent.ClientSpecificLogic.Logging;
using ShipExecAgent.DAL;
using ShipExecAgent.DAL.Managers;
using ShipExecAgent.Services;
using ShipExecAgent.SK;
using ShipExecAgent.Shared.Interfaces;
using ShipExecAgent.Shared.Logging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ShipExecAgent"));

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ShipExecAgent") ?? string.Empty;
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
    builder.Services.AddScoped<VarianceManager>();
    builder.Services.AddScoped<TemplateManager>();
    builder.Services.AddScoped<ApiLogManager>();

    builder.Services.AddScoped<IXmlRepository, XmlRepository>();
    builder.Services.AddScoped<IXmlViewerService, XmlViewerService>();
    builder.Services.AddSingleton<IXmlEnumService, XmlEnumService>();
    builder.Services.AddSingleton<IXmlSchemaService, XmlSchemaService>();
    builder.Services.AddScoped<IShipExecService, ShipExecService>();
    builder.Services.AddScoped<IXmlRefLookupService, XmlRefLookupService>();
    builder.Services.AddSingleton<AlertService>();
    builder.Services.AddScoped<PendingImportService>();
    builder.Services.AddSingleton<IVectorSearchService, InMemoryRagService>();
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<IAiChatService, SemanticKernelChatService>();
    builder.Services.AddScoped<ICbrAnalysisService, CbrAnalysisService>();
    builder.Services.AddSingleton<SummaryPdfService>();

    QuestPDF.Settings.License = LicenseType.Community;

    var app = builder.Build();

    // Wire up the static logger providers used by non-DI classes in library projects.
    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    AppLoggerFactory.Initialize(loggerFactory);
    LoggerProvider.Initialize(loggerFactory);
    ShipExecAgent.ClientSpecificLogic.Logging.LoggerProvider.Initialize(loggerFactory);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, http) =>
        {
            diag.Set("RequestHost", http.Request.Host.Value);
            diag.Set("RequestScheme", http.Request.Scheme);
        };
    });

    // ── Content-Security-Policy: block inline scripts and eval ──────────
    app.Use(async (context, next) =>
    {
        var cspPolicy = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' 'sha256-dQxdeGZjFqPCswiIiPV57iSY1ejBiU7sWgV6E0c1fqw='; style-src 'self' 'unsafe-inline'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*;";

        context.Response.Headers["Content-Security-Policy"] = cspPolicy;
        await next();
    });

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.MapPost("/api/show-alert", async (AlertService alertService, AlertRequest request) =>
    {
        await alertService.ShowAlertAsync(request.Message);
        return Results.Ok();
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

record AlertRequest(string Message);
