using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;

namespace SV22T1020678.Shop.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index(int categoryId = 0, decimal minPrice = 0, decimal maxPrice = 0, string searchValue = "", int page = 1, string sortOrder = "NameASC") 
        {
            var input = new SV22T1020678.Models.Catalog.ProductSearchInput()
            {
                Page = page,
                PageSize = 20,
                SearchValue = searchValue ?? "",
                CategoryID = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sortOrder
            };

            var data = await CatalogDataService.ListProductsAsync(input);

            data.Page = input.Page;
            data.PageSize = input.PageSize;

            ViewBag.Categories = (await CatalogDataService.ListCategoriesAsync(new SV22T1020678.Models.Common.PaginationSearchInput { SearchValue = "" })).DataItems;
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