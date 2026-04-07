using PSI.Sox;
using PSI.Sox.Configuration;
using Serilog;
using ShipExecNavigator.AppLogic;
using ShipExecNavigator.Blazor.Components;
using ShipExecNavigator.BusinessLogic.Logging;
using ShipExecNavigator.ClientSpecificLogic.Logging;
using ShipExecNavigator.DAL;
using ShipExecNavigator.DAL.Managers;
using ShipExecNavigator.Services;
using ShipExecNavigator.SK;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Logging;

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
        .Enrich.WithProperty("Application", "ShipExecNavigator"));

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ShipExecNavigator") ?? string.Empty;
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
    builder.Services.AddSingleton<IVectorSearchService, InMemoryRagService>();
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<IAiChatService, SemanticKernelChatService>();
    builder.Services.AddScoped<ICbrAnalysisService, CbrAnalysisService>();

    var app = builder.Build();

    // Wire up the static logger providers used by non-DI classes in library projects.
    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    AppLoggerFactory.Initialize(loggerFactory);
    LoggerProvider.Initialize(loggerFactory);
    ShipExecNavigator.ClientSpecificLogic.Logging.LoggerProvider.Initialize(loggerFactory);

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
