using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using System.Security.Claims;

namespace SV22T1020678.Admin.Controllers
{
    // PHẢI CÓ AuthenticationSchemes mới hết bị đá ra Login nhé Nghĩa
    [Authorize(AuthenticationSchemes = "AdminWebAuth")]
    public class AccountController : Controller
    {
        private const string AUTH_SCHEME = "AdminWebAuth";

        [HttpGet]
        [AllowAnonymous] // Phải có cái này để vào được trang Login
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
            var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(username, password);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Sai tài khoản hoặc mật khẩu!");
                return View();
            }

            // KIỂM TRA ROLE (Trên Git cậu dùng RoleNames)
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, userAccount.UserName),
                new Claim(ClaimTypes.GivenName, userAccount.DisplayName),
                new Claim(ClaimTypes.Email, userAccount.Email ?? ""),
                new Claim("Photo", userAccount.Photo ?? "nophoto.png")
            };

            // Tách quyền và add vào Claim
            if (!string.IsNullOrEmpty(userAccount.RoleNames))
            {
                foreach (var role in userAccount.RoleNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                }
            }

            var identity = new ClaimsIdentity(claims, AUTH_SCHEME);
            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập đúng Scheme
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
                ViewBag.Error = "Nhập đủ mật khẩu sếp ơi!";
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
                ViewBag.Success = "Đổi mật khẩu thành công rực rỡ!";
                return View();
            }

            ViewBag.Error = "Lỗi hệ thống, thử lại sau.";
            return View();
        }
    }
}