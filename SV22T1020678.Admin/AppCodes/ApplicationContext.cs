using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SV22T1020678.Admin.AppCodes
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng web
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Gọi hàm này trong Program.cs
        /// </summary>
        public static void Configure(
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException();
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException();
            _configuration = configuration ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// HttpContext hiện tại
        /// </summary>
        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;

        /// <summary>
        /// Environment của Web
        /// </summary>
        public static IWebHostEnvironment? WebHostEnviroment => _webHostEnvironment;

        /// <summary>
        /// Configuration
        /// </summary>
        public static IConfiguration? Configuration => _configuration;

        /// <summary>
        /// URL gốc của website
        /// </summary>
        public static string BaseURL =>
            $"{HttpContext?.Request.Scheme}://{HttpContext?.Request.Host}/";

        /// <summary>
        /// Đường dẫn vật lý tới thư mục wwwroot
        /// </summary>
        public static string WWWRootPath =>
            _webHostEnvironment?.WebRootPath ?? "";

        /// <summary>
        /// Đường dẫn vật lý tới thư mục gốc ứng dụng
        /// </summary>
        public static string ApplicationRootPath =>
            _webHostEnvironment?.ContentRootPath ?? "";

        /// <summary>
        /// Ghi dữ liệu vào Session
        /// </summary>
        public static void SetSessionData(string key, object value)
        {
            try
            {
                string sValue = JsonConvert.SerializeObject(value);
                if (!string.IsNullOrEmpty(sValue))
                {
                    _httpContextAccessor?
                        .HttpContext?
                        .Session
                        .SetString(key, sValue);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Đọc dữ liệu từ Session
        /// </summary>
        public static T? GetSessionData<T>(string key) where T : class
        {
            try
            {
                string sValue =
                    _httpContextAccessor?
                    .HttpContext?
                    .Session
                    .GetString(key) ?? "";

                if (!string.IsNullOrEmpty(sValue))
                {
                    return JsonConvert.DeserializeObject<T>(sValue);
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Lấy giá trị cấu hình trong appsettings.json
        /// </summary>
        public static string GetConfigValue(string name)
        {
            return _configuration?[name] ?? "";
        }

        /// <summary>
        /// Lấy section cấu hình
        /// </summary>
        public static T GetConfigSection<T>(string name) where T : new()
        {
            var value = new T();
            _configuration?.GetSection(name).Bind(value);
            return value;
        }
    }
}