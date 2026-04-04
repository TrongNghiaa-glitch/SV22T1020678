using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SV22T1020678.Admin.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        [AllowAnonymous] // Cho phép ai cũng vào được trang đăng nhập
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ Email và Mật khẩu!");
                return View();
            }

            // TODO: Chỗ này sau này bạn thay bằng hàm check Database
            if (email == "Nhi@gmail.com" && password == "123")
            {
                // 1. Khởi tạo danh sách các thông tin (Claims) của người dùng
                var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Name, "Nhi"), // Tên hiển thị
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, "Admin") // Cấp quyền Admin
                };

                // 2. Tạo Identity và Principal
                var identity = new ClaimsIdentity(claims, "AdminWebAuth");
                var principal = new ClaimsPrincipal(identity);

                // 3. Ghi nhận đăng nhập bằng Cookie
                await HttpContext.SignInAsync("AdminWebAuth", principal);

                // Đăng nhập thành công -> Chuyển về Dashboard
                return RedirectToAction("Index", "Home"); // Sửa lại "Dashboard" thành "Home" nếu project bạn dùng HomeController
            }

            // Đăng nhập thất bại
            ModelState.AddModelError("Error", "Tài khoản hoặc mật khẩu không chính xác!");
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            // Xóa Cookie đăng xuất
            await HttpContext.SignOutAsync("AdminWebAuth");
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize] // Bắt buộc phải đăng nhập mới được vào trang đổi mật khẩu
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không trùng khớp.";
                return View();
            }

            // TODO: Thêm logic kiểm tra mật khẩu cũ từ Database tại đây

            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }
    }
}