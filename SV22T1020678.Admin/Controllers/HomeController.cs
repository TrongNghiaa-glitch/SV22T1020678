using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.Admin.Models;

namespace SV22T1020678.Admin.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Hàm kh?i t?o c?a HomeController, t? ??ng tiêm (Dependency Injection) d?ch v? logging.
        /// </summary>
        /// <param name="logger">D?ch v? ghi log c?a h? th?ng ?? theo dõi ho?t ??ng và l?i.</param>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hi?n th? trang ch? (Dashboard) c?a h? th?ng qu?n tr? viên.
        /// N?i th??ng ch?a các bi?u ?? th?ng kê, thông tin t?ng quan.
        /// </summary>
        /// <returns>Tr? v? View trang ch? (Index).</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Hi?n th? trang thông tin v? chính sách b?o m?t (Privacy Policy) c?a h? th?ng.
        /// </summary>
        /// <returns>Tr? v? View ch?a n?i dung chính sách b?o m?t.</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Hi?n th? trang thông báo l?i chung c?a h? th?ng khi có Exception (ngo?i l?) không mong mu?n x?y ra.
        /// </summary>
        /// <returns>Tr? v? View "Error" kèm theo mã ??nh danh c?a request (RequestId) ?? l?p trình viên d? dàng tra c?u (debug) l?i.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}