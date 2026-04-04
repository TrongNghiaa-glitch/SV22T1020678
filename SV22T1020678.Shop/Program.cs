using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 1. KHỞI TẠO DATABASE TỪ SỚM (RẤT QUAN TRỌNG)
// =======================================================
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("Lỗi: Không tìm thấy chuỗi kết nối 'LiteCommerceDB' trong appsettings.json");
SV22T1020678.BusinessLayers.Configuration.Initialize(connectionString);

// =======================================================
// 2. KHAI BÁO CÁC DỊCH VỤ (SERVICES)
// =======================================================
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

// Authentication (Đã đổi tên Cookie sang Shop để không bị đá văng tài khoản bên Admin)
builder.Services.AddAuthentication("AdminWebAuth")
    .AddCookie("AdminWebAuth", option =>
    {
        option.Cookie.Name = "LiteCommerce.Shop";
        option.LoginPath = "/Account/Login";
        option.AccessDeniedPath = "/Account/AccessDenied";
        option.ExpireTimeSpan = TimeSpan.FromDays(7);
        option.SlidingExpiration = true;
        option.Cookie.HttpOnly = true;
        option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

// =======================================================
// 3. BUILD ỨNG DỤNG (CHỈ ĐƯỢC GỌI 1 LẦN DUY NHẤT Ở ĐÂY)
// =======================================================
var app = builder.Build();

// =======================================================
// 4. CẤU HÌNH PIPELINE LƯU THÔNG
// =======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Thứ tự này bắt buộc phải chuẩn: Xác thực -> Phân quyền -> Session
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Culture (Việt Nam)
var supportedCultures = new[] { new CultureInfo("vi-VN") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi-VN"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Chạy Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();