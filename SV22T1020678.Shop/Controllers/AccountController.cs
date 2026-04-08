using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Partner;
using SV22T1020678.Models.Security;
using System.Security.Claims;

namespace SV22T1020678.Shop.Controllers
{
    [Authorize(AuthenticationSchemes = "CustomerWebAuth")]
    public class AccountController : Controller
    {
        private const string AUTH_SCHEME = "CustomerWebAuth";

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập Email và Mật khẩu");
                return View();
            }

            // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
            var userData = await SecurityDataService.AuthorizeCustomerAsync(email, password);

            if (userData == null)
            {
                // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
                var empData = await SecurityDataService.AuthorizeEmployeeAsync(email, password);

                if (empData != null)
                {
                    var newCustomer = new Customer()
                    {
                        CustomerName = empData.DisplayName,
                        ContactName = empData.DisplayName,
                        Email = empData.Email,
                        Phone = "0123456789",
                        Address = "Nhân viên nội bộ",
                        Province = "Thừa Thiên Huế",
                        IsLocked = false
                    };

                    // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
                    int newCustomerId = await PartnerDataService.AddCustomerAsync(newCustomer);

                    if (newCustomerId > 0)
                    {
                        // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
                        await SecurityDataService.ChangeCustomerPasswordAsync(empData.Email, password);

                        // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
                        userData = await SecurityDataService.AuthorizeCustomerAsync(email, password);
                    }
                }
            }

            if (userData == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng!");
                return View();
            }

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, userData.DisplayName ?? ""),
                new Claim(ClaimTypes.Email, userData.Email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, userData.UserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, AUTH_SCHEME);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(AUTH_SCHEME, principal);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AUTH_SCHEME);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        // SỬA DÒNG NÀY: Đổi từ IActionResult thành async Task<IActionResult>
        public async Task<IActionResult> Register()
        {
            // Chỗ này sếp giữ nguyên (đã có await)
            ViewBag.Provinces = await DictionaryDataService.ListOfProvinces();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer data)
        {
            if (string.IsNullOrWhiteSpace(data.CustomerName) || string.IsNullOrWhiteSpace(data.Email))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                ViewBag.Provinces = await DictionaryDataService.ListOfProvinces();
                return View(data);
            }

            // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
            int id = await PartnerDataService.AddCustomerAsync(data);
            if (id > 0) return RedirectToAction("Login");

            ViewBag.Error = "Đăng ký thất bại hoặc Email đã tồn tại!";
            ViewBag.Provinces = DictionaryDataService.ListOfProvinces();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int customerId))
            {
                var current = await PartnerDataService.GetCustomerAsync(customerId);
                ViewBag.Provinces = await DictionaryDataService.ListOfProvinces();
                return View(current);
            }
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Customer data)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int customerId))
            {
                // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
                var current = await PartnerDataService.GetCustomerAsync(customerId);
                if (current != null)
                {
                    current.CustomerName = data.CustomerName;
                    current.ContactName = data.ContactName;
                    current.Phone = data.Phone;
                    current.Address = data.Address;
                    current.Province = data.Province;

                    // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
                    await PartnerDataService.UpdateCustomerAsync(current);

                    var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, current.CustomerName ?? ""),
                        new Claim(ClaimTypes.NameIdentifier, current.CustomerID.ToString()),
                        new Claim(ClaimTypes.Email, current.Email ?? ""),
                        new Claim(ClaimTypes.Role, "customer")
                    };
                    var identity = new ClaimsIdentity(claims, AUTH_SCHEME);
                    await HttpContext.SignInAsync(AUTH_SCHEME, new ClaimsPrincipal(identity));

                    TempData["Message"] = "Cập nhật thông tin thành công!";
                }
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Xác nhận mật khẩu không khớp!";
                return RedirectToAction("Profile");
            }

            string email = User.FindFirstValue(ClaimTypes.Email) ?? "";

            // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
            var check = await SecurityDataService.AuthorizeCustomerAsync(email, oldPassword);
            if (check == null)
            {
                TempData["Error"] = "Mật khẩu cũ không đúng!";
                return RedirectToAction("Profile");
            }

            // ĐÃ THÊM AWAIT VÀ CHỮ ASYNC Ở ĐUÔI
            bool result = await SecurityDataService.ChangeCustomerPasswordAsync(email, newPassword);
            if (result)
                TempData["Message"] = "Đổi mật khẩu thành công!";
            else
                TempData["Error"] = "Lỗi hệ thống!";

            return RedirectToAction("Profile");
        }
    }
}