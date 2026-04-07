using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using System.Security.Claims;

namespace SV22T1020678.Admin.Controllers
{
    // BẮT BUỘC phải có chữ AuthenticationSchemes mới hết bị đá ra Login
    [Authorize(AuthenticationSchemes = "AdminWebAuth")]
    public class AccountController : Controller
    {
        private const string AUTH_SCHEME = "AdminWebAuth";

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Nhập đủ tài khoản và mật khẩu nhé Nghĩa!");
                return View();
            }

            var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(username, password);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Sai tài khoản hoặc mật khẩu!");
                return View();
            }

            // KIỂM TRA ROLE: Lưu ý chữ 'admin' phải khớp với DB của cậu
            string roles = userAccount.RoleNames?.ToLower() ?? "";
            if (!roles.Contains("admin") && !roles.Contains("employee"))
            {
                ModelState.AddModelError("Error", "Bạn không có quyền vào trang Quản trị!");
                return View();
            }

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, userAccount.UserName),
                new Claim(ClaimTypes.GivenName, userAccount.DisplayName),
                new Claim(ClaimTypes.Email, userAccount.Email ?? ""),
                new Claim("Photo", userAccount.Photo ?? "nophoto.png")
            };

            foreach (var role in userAccount.RoleNames.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }

            var identity = new ClaimsIdentity(claims, AUTH_SCHEME);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(AUTH_SCHEME, principal);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AUTH_SCHEME);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error = "Nhập đủ các ô mật khẩu sếp ơi!";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            string userName = User.Identity?.Name ?? "";

            // Check pass cũ
            var check = await SecurityDataService.AuthorizeEmployeeAsync(userName, oldPassword);
            if (check == null)
            {
                ViewBag.Error = "Mật khẩu cũ không chính xác!";
                return View();
            }

            // Đổi pass
            bool result = await SecurityDataService.ChangeEmployeePasswordAsync(userName, newPassword);
            if (result)
            {
                ViewBag.Success = "Đổi mật khẩu thành công!";
                return View();
            }

            ViewBag.Error = "Lỗi hệ thống, thử lại sau nhé.";
            return View();
        }
    }
}