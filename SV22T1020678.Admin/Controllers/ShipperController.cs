using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Thư viện cần thiết để dùng Session
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System;
using System.Threading.Tasks;

namespace SV22T1020678.Admin.Controllers
{
    public class ShipperController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "ShipperSearchCondition"; // Biến lưu Session

        // Hàm Index chỉ làm nhiệm vụ nạp giao diện và khôi phục Session
        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Người giao hàng";

            // Khôi phục điều kiện tìm kiếm từ session (Bookmark)
            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";

            return View();
        }

        // Hàm Search làm nhiệm vụ tìm kiếm AJAX và trả về PartialView
        [HttpPost]
        public async Task<IActionResult> Search(PaginationSearchInput condition)
        {
            // Lưu lại điều kiện vào session
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", condition.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", condition.SearchValue ?? "");

            condition.PageSize = PAGE_SIZE;

            var data = await PartnerDataService.ListShippersAsync(condition);
            return PartialView("Search", data);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var model = new Shipper() { ShipperID = 0 };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Shipper data)
        {
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";
                return View("Edit", data);
            }

            try
            {
                if (data.ShipperID == 0)
                    await PartnerDataService.AddShipperAsync(data);
                else
                    await PartnerDataService.UpdateShipperAsync(data);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.IsUsed = await PartnerDataService.IsUsedShipperAsync(id);
            ViewBag.Title = "Xóa người giao hàng";
            return View(model);
        }
    }
}