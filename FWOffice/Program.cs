using FWOffice.Components;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using StandOrder.Models;
using System.Text;

// Required by ExcelDataReader to read legacy .xls files.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Shared FWDB data layer — same SQL Server database as the warehouse app.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<CircuitOptions>(options => options.DetailedErrors = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Served from the /office subfolder under IIS (mirrors the warehouse app's /orders).
    app.UsePathBase("/office");
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
