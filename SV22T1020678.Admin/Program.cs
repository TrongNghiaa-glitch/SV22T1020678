using SV22T1020678.Admin.AppCodes;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using Microsoft.AspNetCore.Localization; 

var builder = WebApplication.CreateBuilder(args);

// =======================
// Add services
// =======================
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

// 1. Authentication (Đã đồng bộ tên "AdminWebAuth" khớp với AccountController)
builder.Services.AddAuthentication("AdminWebAuth")
    .AddCookie("AdminWebAuth", option =>
    {
        option.Cookie.Name = "LiteCommerce.Admin";
        option.LoginPath = "/Account/Login";
        option.AccessDeniedPath = "/Account/AccessDenied";
        option.ExpireTimeSpan = TimeSpan.FromDays(7);
        option.SlidingExpiration = true;
        option.Cookie.HttpOnly = true;
        option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// 2. Session (Bổ sung bộ nhớ đệm cho Session)
builder.Services.AddDistributedMemoryCache(); // <--- Dòng mới thêm
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

var app = builder.Build();

// =======================
// Configure pipeline
// =======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Thêm HTTPS redirection cho môi trường Production (Bảo mật)
    app.UseHsts();
}

app.UseHttpsRedirection(); // <--- Dòng mới thêm (Chuyển hướng http sang https)
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// =======================
// Culture (Việt Nam) - Cách chuẩn của ASP.NET Core
// =======================
var supportedCultures = new[] { new CultureInfo("vi-VN") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi-VN"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// =======================
// Application Context & Business Layer
// =======================

ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    app.Configuration
);

string connectionString = app.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");

SV22T1020678.BusinessLayers.Configuration.Initialize(connectionString);

// =======================
// Routing & Run
// =======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();