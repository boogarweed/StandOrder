using System.Text;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using StandOrder.Components;
using StandOrder.Models;
using StandOrder.Services;

// Required by ExcelDataReader to read legacy .xls files.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<CircuitOptions>(options => options.DetailedErrors = true);

builder.Services.AddScoped<TruckDataService>();
builder.Services.AddScoped<PriceSheetImportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UsePathBase("/orders");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

// Razor Components root
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
