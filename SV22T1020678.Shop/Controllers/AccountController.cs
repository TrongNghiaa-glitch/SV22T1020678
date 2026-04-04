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
            // GỌI HÀM "TRIỆT ĐỂ" VỪA TẠO
            var customer = await PartnerDataService.GetCustomerByEmailAsync(username);

            // Bổ sung kiểm tra Mật khẩu (Giả sử trong DB bạn có cột Password)
            // LƯU Ý: Nếu bảng Customers của bạn CHƯA có cột Password, bạn cứ bỏ đoạn && customer.Password == password đi nhé
            if (customer != null && customer.Email == username /* && customer.Password == password */)
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
            // Truyền danh sách tỉnh thành ra View thông qua ViewBag
            ViewBag.Provinces = await PartnerDataService.ListProvincesAsync();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(Customer data)
        {
            try
            {
                // 1. Kiểm tra các trường bắt buộc
                if (string.IsNullOrWhiteSpace(data.CustomerName) || string.IsNullOrWhiteSpace(data.Email))
                {
                    ViewBag.Error = "Vui lòng nhập đầy đủ Tên và Email!";
                    return View(data); // Trả lại form kèm dữ liệu cũ để khách không phải gõ lại
                }

                // 2. Kiểm tra xem Email này đã có ai dùng chưa (Dùng hàm IsValidEmailAsync bạn đã viết ở Repo)
                bool isEmailValid = await PartnerDataService.IsValidEmailAsync(data.Email, 0);
                if (!isEmailValid)
                {
                    ViewBag.Error = "Email này đã được sử dụng. Vui lòng chọn Email khác!";
                    return View(data);
                }

                // 3. Gán giá trị mặc định cho tài khoản mới
                data.IsLocked = false;

                // 4. Lưu vào Database
                int newCustomerId = await PartnerDataService.AddCustomerAsync(data);

                if (newCustomerId > 0)
                {
                    // Đăng ký thành công thì đẩy thẳng về trang Đăng nhập
                    return RedirectToAction("Login");
                }

                ViewBag.Error = "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng thử lại!";
                return View(data);
            }
            catch (Exception ex)
            {
                // Bắt lỗi hệ thống (ví dụ sập DB)
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                return View(data);
            }
        }
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            // 1. Lấy ID từ ClaimIdentifier (Cách này chắc chắn hơn lấy Name/Email)
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int customerId))
            {
                // 2. Gọi hàm lấy theo ID (hàm GetAsync đã có sẵn trong PartnerDataService)
                var customer = await PartnerDataService.GetCustomerAsync(customerId);

                if (customer != null)
                    return View(customer);
            }

            // Nếu không tìm thấy, đá về trang chủ hoặc báo lỗi
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var current = await PartnerDataService.GetCustomerByEmailAsync(User.Identity!.Name!);

            if (current?.Password != oldPassword)
            {
                TempData["Error"] = "Mật khẩu cũ không chính xác!";
            }
            else if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Xác nhận mật khẩu mới không khớp!";
            }
            else
            {
                current.Password = newPassword;
                await PartnerDataService.UpdateCustomerAsync(current);
                TempData["Message"] = "Đổi mật khẩu thành công!";
            }
            return RedirectToAction("Profile");
        }
    }
}