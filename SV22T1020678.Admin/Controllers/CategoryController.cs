using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using System;
using System.Threading.Tasks;

namespace SV22T1020678.Admin.Controllers
{
    public class CategoryController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "CategorySearchCondition";

        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Loại hàng";

            // Khôi phục Session tìm kiếm bằng ViewBag để tránh lỗi NullReferenceException ở View
            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(PaginationSearchInput condition)
        {
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", condition.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", condition.SearchValue ?? "");

            condition.PageSize = PAGE_SIZE;

            // Dùng hàm ListOfCategories từ DictionaryDataService (hoặc CatalogDataService tùy cấu trúc của bạn)
            var data = await DictionaryDataService.ListOfCategories(condition);
            return PartialView("Search", data);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Loại hàng";
            var model = new Category()
            {
                CategoryID = 0,
                CategoryName = string.Empty, // Chống cảnh báo null literal
                Description = string.Empty
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật Loại hàng";
            var model = await DictionaryDataService.GetCategory(id);
            if (model == null) return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Category data)
        {
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

            data.Description = data.Description ?? string.Empty;

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung Loại hàng" : "Cập nhật Loại hàng";
                return View("Edit", data);
            }

            try
            {
                if (data.CategoryID == 0)
                    await DictionaryDataService.AddCategory(data);
                else
                    await DictionaryDataService.UpdateCategory(data);

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
                bool result = await DictionaryDataService.DeleteCategory(id);
                if (!result)
                {
                    ModelState.AddModelError("", "Không thể xóa loại hàng này vì đang có sản phẩm liên quan (ràng buộc dữ liệu)!");
                    var reloadModel = await DictionaryDataService.GetCategory(id);
                    return View(reloadModel);
                }
                return RedirectToAction("Index");
            }

            var model = await DictionaryDataService.GetCategory(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.Title = "Xóa Loại hàng";
            return View(model);
        }
    }
}