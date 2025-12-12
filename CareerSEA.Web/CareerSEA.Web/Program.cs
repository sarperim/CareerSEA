using CareerSEA.Web;
using CareerSEA.Web.Client.Pages;
using CareerSEA.Web.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Services.AddHttpClient<ServerApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true) 
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<PredictionState>();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<ProtectedSessionStorage>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CareerSEA.Web.Client._Imports).Assembly);

app.Run();
