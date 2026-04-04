using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System;
using System.Threading.Tasks;

namespace SV22T1020678.Admin.Controllers
{
    public class CustomerController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "CustomerSearchCondition";

        // ==========================================
        // 1. HIỂN THỊ DANH SÁCH & TÌM KIẾM AJAX
        // ==========================================
        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Khách hàng";

            // Khôi phục Session tìm kiếm
            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(PaginationSearchInput condition)
        {
            // Lưu điều kiện vào Session
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", condition.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", condition.SearchValue ?? "");

            condition.PageSize = PAGE_SIZE;

            var data = await PartnerDataService.ListCustomersAsync(condition);
            return PartialView("Search", data);
        }

        // ==========================================
        // 2. THÊM / CẬP NHẬT KHÁCH HÀNG
        // ==========================================
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";

            // Khởi tạo model với chuỗi rỗng để tránh lỗi Warning NullReference
            var model = new Customer()
            {
                CustomerID = 0,
                IsLocked = false,
                CustomerName = string.Empty,
                ContactName = string.Empty,
                Phone = string.Empty,
                Email = string.Empty,
                Address = string.Empty,
                Province = string.Empty
            };

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);

            if (model == null) return RedirectToAction("Index");

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Customer data)
        {
            // Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");

            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành");

            // Xử lý giá trị rỗng cho trường không bắt buộc
            data.Address = string.IsNullOrWhiteSpace(data.Address) ? "" : data.Address;

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View("Edit", data);
            }

            try
            {
                if (data.CustomerID == 0)
                    await PartnerDataService.AddCustomerAsync(data);
                else
                    await PartnerDataService.UpdateCustomerAsync(data);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View("Edit", data);
            }
        }
        // ==========================================
        // 3. XÓA KHÁCH HÀNG
        // ==========================================
        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                // Kiểm tra lại lần nữa trước khi xóa: Nếu khách hàng đang có đơn hàng thì không cho xóa
                bool isUsed = await PartnerDataService.IsUsedCustomerAsync(id);
                if (isUsed)
                {
                    // Chặn hành vi cố tình gọi POST, đẩy về lại trang Index
                    return RedirectToAction("Index");
                }

                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");

            // Truyền cờ IsUsed ra ngoài View để quyết định ẩn/hiện nút Xóa
            ViewBag.IsUsed = await PartnerDataService.IsUsedCustomerAsync(id);
            ViewBag.Title = "Xóa khách hàng";

            return View(model);
        }

        // ==========================================
        // 4. ĐỔI MẬT KHẨU KHÁCH HÀNG
        // ==========================================
        public async Task<IActionResult> ChangePassword(int id = 0)
        {
            ViewBag.Title = "Đổi mật khẩu khách hàng";

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePassword(int customerID, string newPassword, string confirmPassword)
        {
            var model = await PartnerDataService.GetCustomerAsync(customerID);
            if (model == null) return RedirectToAction("Index");

            // Kiểm tra dữ liệu hợp lệ
            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới.");

            if (string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError("confirmPassword", "Vui lòng xác nhận lại mật khẩu.");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Đổi mật khẩu khách hàng";
                return View("ChangePassword", model);
            }

            try
            {
                // TODO: Gọi hàm UpdatePassword ở DataService để lưu vào Database.
                // await PartnerDataService.UpdateCustomerPasswordAsync(customerID, newPassword);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return View("ChangePassword", model);
            }
        }
    }
}