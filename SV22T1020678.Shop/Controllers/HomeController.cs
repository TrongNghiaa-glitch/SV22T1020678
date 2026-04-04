using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Catalog; // Thêm thư mục Catalog chứa Product
using SV22T1020678.Models.Common;  // Thêm thư mục Common chứa PagedResult

namespace SV22T1020678.Shop.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index(int categoryId = 0, string searchValue = "", decimal minPrice = 0, decimal maxPrice = 0, int page = 1)
        {
            var input = new ProductSearchInput()
            {
                Page = page,
                PageSize = 24,
                SearchValue = searchValue ?? "",
                CategoryID = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var data = await CatalogDataService.ListProductsAsync(input);

            // Sửa lỗi CS1503: Truyền đúng kiểu PaginationSearchInput vào hàm
            // Thêm .DataItems để bóc lấy danh sách truyền ra ngoài View
            ViewBag.Categories = (await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { SearchValue = "" })).DataItems;
            ViewBag.SearchInput = input;

            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            return View(product);
        }
    }
}