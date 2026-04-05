using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System.Security.Claims;

namespace SV22T1020678.Shop.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var customer = await PartnerDataService.GetCustomerByEmailAsync(username);

            // Kiểm tra thông tin đăng nhập
            if (customer != null && customer.Email == username && customer.Password == password)
            {
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, customer.CustomerName ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                    new Claim(ClaimTypes.Email, customer.Email ?? "")
                };
                var identity = new ClaimsIdentity(claims, "AdminWebAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("AdminWebAuth", principal);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tài khoản hoặc mật khẩu không đúng!";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminWebAuth");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // Sử dụng GetProvincesAsync từ PartnerDataService
            ViewBag.Provinces = CatalogDataService.ListOfProvinces();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Customer data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.CustomerName) || string.IsNullOrWhiteSpace(data.Email))
                {
                    ViewBag.Error = "Vui lòng nhập đầy đủ Tên và Email!";
                    return View(data);
                }

                bool isEmailValid = await PartnerDataService.IsValidEmailAsync(data.Email, 0);
                if (!isEmailValid)
                {
                    ViewBag.Error = "Email này đã được sử dụng. Vui lòng chọn Email khác!";
                    return View(data);
                }

                data.IsLocked = false;
                int newCustomerId = await PartnerDataService.AddCustomerAsync(data);

                if (newCustomerId > 0)
                {
                    return RedirectToAction("Login");
                }

                ViewBag.Error = "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng thử lại!";
                return View(data);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                return View(data);
            }
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int customerId))
            {
                var current = await PartnerDataService.GetCustomerAsync(customerId);

                // LẤY DANH SÁCH TỪ REPO (Đảm bảo khớp 100% với khóa ngoại)
                ViewBag.Provinces = await PartnerDataService.GetProvincesAsync();

                return View(current);
            }
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int customerId))
            {
                var current = await PartnerDataService.GetCustomerAsync(customerId);

                if (current != null)
                {
                    string dbPass = (current.Password ?? "").Trim();
                    string inputOldPass = (oldPassword ?? "").Trim();

                    bool isOldAccount = string.IsNullOrEmpty(dbPass);

                    if (!isOldAccount && dbPass != inputOldPass)
                    {
                        TempData["Error"] = "Mật khẩu hiện tại không chính xác!";
                    }
                    else if ((newPassword ?? "").Trim() != (confirmPassword ?? "").Trim())
                    {
                        TempData["Error"] = "Xác nhận mật khẩu mới không khớp!";
                    }
                    else if (string.IsNullOrEmpty((newPassword ?? "").Trim()))
                    {
                        TempData["Error"] = "Vui lòng nhập mật khẩu mới!";
                    }
                    else
                    {
                        current.Password = newPassword.Trim();
                        await PartnerDataService.UpdateCustomerAsync(current);
                        TempData["Message"] = "Đổi mật khẩu thành công!";
                    }
                }
            }
            else
            {
                TempData["Error"] = "Lỗi xác thực người dùng. Vui lòng đăng nhập lại!";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(Customer data)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int customerId))
            {
                var current = await PartnerDataService.GetCustomerAsync(customerId);

                if (current != null)
                {
                    current.CustomerName = data.CustomerName ?? current.CustomerName;
                    current.ContactName = data.ContactName ?? current.ContactName;
                    current.Phone = data.Phone ?? current.Phone;
                    current.Address = data.Address ?? current.Address;

                    if (!string.IsNullOrEmpty(data.Province))
                    {
                        current.Province = data.Province;
                    }

                    await PartnerDataService.UpdateCustomerAsync(current);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, current.CustomerName),
                        new Claim(ClaimTypes.Email, current.Email),
                        new Claim(ClaimTypes.NameIdentifier, current.CustomerID.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, "AdminWebAuth");
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync("AdminWebAuth", principal);

                    TempData["Message"] = "Cập nhật thông tin thành công!";
                }
            }

            return RedirectToAction("Profile");
        }
    }
}