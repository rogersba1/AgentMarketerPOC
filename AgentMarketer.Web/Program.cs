using AgentMarketer.Web.Components;
using AgentMarketer.Web.Services;
using AgentMarketer.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient for API calls
builder.Services.AddHttpClient("AgentMarketerAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:7282");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register HTTP client-based services
builder.Services.AddScoped<ICampaignService, CampaignHttpService>();
builder.Services.AddScoped<IApprovalService, ApprovalHttpService>();
builder.Services.AddScoped<ChatOrchestrationService>();

// Add SignalR client services
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
