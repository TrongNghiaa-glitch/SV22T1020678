using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.HR;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SV22T1020678.Admin.Controllers
{
    public class EmployeeController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "EmployeeSearchCondition";

        // ==========================================
        // 1. HIỂN THỊ DANH SÁCH & TÌM KIẾM AJAX
        // ==========================================
        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Nhân viên";

            var input = new PaginationSearchInput()
            {
                Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1,
                PageSize = PAGE_SIZE,
                SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? ""
            };

            return View(input);
        }

        [HttpPost]
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", input.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", input.SearchValue ?? "");

            input.PageSize = PAGE_SIZE;
            var data = await DictionaryDataService.ListOfEmployees(input);
            return PartialView("Search", data);
        }

        // ==========================================
        // 2. THÊM / SỬA NHÂN VIÊN
        // ==========================================
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true,
                FullName = "",
                Email = "",
                Address = "",
                Phone = "",
                Photo = "nophoto.png"
            };

            // Xử lý riêng cho RoleNames nếu có trong Model để tránh lỗi null literal
            if (model.GetType().GetProperty("RoleNames") != null)
            {
                model.GetType().GetProperty("RoleNames").SetValue(model, "");
            }

            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await DictionaryDataService.GetEmployee(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                // Kiểm tra dữ liệu đầu vào bắt buộc
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else
                {
                    // Kiểm tra Email có bị trùng lặp không
                    bool isValidEmail = await DictionaryDataService.ValidateEmployeeEmail(data.Email, data.EmployeeID);
                    if (!isValidEmail)
                        ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");
                }

                if (!ModelState.IsValid)
                    return View("Edit", data);

                // Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "employees", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                // Tiền xử lý dữ liệu tránh null trước khi lưu vào DB
                data.Address = data.Address ?? "";
                data.Phone = data.Phone ?? "";
                data.Photo = string.IsNullOrWhiteSpace(data.Photo) ? "nophoto.png" : data.Photo;

                // Nếu model có RoleNames thì cũng chống null cho nó
                if (data.GetType().GetProperty("RoleNames") != null)
                {
                    var currentRole = data.GetType().GetProperty("RoleNames").GetValue(data) as string;
                    data.GetType().GetProperty("RoleNames").SetValue(data, currentRole ?? "");
                }

                // Lưu dữ liệu vào Database
                if (data.EmployeeID == 0)
                {
                    await DictionaryDataService.AddEmployee(data);
                }
                else
                {
                    await DictionaryDataService.UpdateEmployee(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Lỗi: " + ex.Message);
                return View("Edit", data);
            }
        }

        // ==========================================
        // 3. XÓA NHÂN VIÊN
        // ==========================================
        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                await DictionaryDataService.DeleteEmployee(id);
                return RedirectToAction("Index");
            }

            var model = await DictionaryDataService.GetEmployee(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.IsUsed = await DictionaryDataService.InUsedEmployee(id);
            ViewBag.Title = "Xóa nhân viên";
            return View(model);
        }

        // ==========================================
        // 4. ĐỔI MẬT KHẨU
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id = 0)
        {
            ViewBag.Title = "Đổi mật khẩu nhân viên";
            var model = await DictionaryDataService.GetEmployee(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePassword(int employeeID, string newPassword, string confirmPassword)
        {
            var employee = await DictionaryDataService.GetEmployee(employeeID);
            if (employee == null) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Đổi mật khẩu nhân viên";
                return View("ChangePassword", employee);
            }

            // Gọi hàm đổi mật khẩu ở tầng DB nếu đã được định nghĩa
            // Ví dụ: await SecurityDataService.ChangeEmployeePasswordAsync(employee.Email, newPassword);

            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. PHÂN QUYỀN NHÂN VIÊN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id = 0)
        {
            ViewBag.Title = "Phân quyền nhân viên";
            var model = await DictionaryDataService.GetEmployee(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveRole(int employeeID, string[] roles)
        {
            string roleNames = (roles != null && roles.Length > 0) ? string.Join(",", roles) : "";
            var employee = await DictionaryDataService.GetEmployee(employeeID);

            if (employee != null)
            {
                // Gán RoleNames thông qua reflection để an toàn nếu model không hỗ trợ set trực tiếp
                var prop = employee.GetType().GetProperty("RoleNames");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(employee, roleNames);
                }

                await DictionaryDataService.UpdateEmployee(employee);
            }
            return RedirectToAction("Index");
        }
    }
}