using Jobick.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Jobick.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TaskService>();

// --- ADD AUTHENTICATION ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/ADHMMC/Login"; // Redirect if not authenticated
        options.AccessDeniedPath = "/ADHMMC/Error403";
    });

builder.Services.AddAuthorization();

// Add this before registering your services
builder.Services.AddDbContext<AdhmmcPmContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ADHMMC}/{action=Index}/{id?}");
//pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
