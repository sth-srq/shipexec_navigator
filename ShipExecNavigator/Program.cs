using PSI.Sox;
using PSI.Sox.Configuration;
using ShipExecNavigator.AppLogic;
using ShipExecNavigator.Components;
using ShipExecNavigator.DAL;
using ShipExecNavigator.DAL.Managers;
using ShipExecNavigator.Services;
using ShipExecNavigator.SK;
using ShipExecNavigator.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ShipExecNavigator") ?? string.Empty;
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<VarianceManager>();
builder.Services.AddScoped<TemplateManager>();

builder.Services.AddScoped<IXmlRepository, XmlRepository>();
builder.Services.AddScoped<IXmlViewerService, XmlViewerService>();
builder.Services.AddSingleton<IXmlEnumService, XmlEnumService>();
builder.Services.AddSingleton<IXmlSchemaService, XmlSchemaService>();
builder.Services.AddScoped<IShipExecService, ShipExecService>();
builder.Services.AddScoped<IXmlRefLookupService, XmlRefLookupService>();
builder.Services.AddSingleton<AlertService>();
builder.Services.AddSingleton<IVectorSearchService, InMemoryRagService>();
builder.Services.AddScoped<IAiChatService, SemanticKernelChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

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

record AlertRequest(string Message);
