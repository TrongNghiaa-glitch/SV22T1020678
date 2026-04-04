using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SV22T1020678.Admin.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGESIZE = 20;
        private const string SEARCH_CONDITION = "ProductSearchCondition"; // Biến lưu Session

        // ==========================================
        // PHẦN 1: QUẢN LÝ THÔNG TIN SẢN PHẨM (CƠ BẢN)
        // ==========================================

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Quản lý Sản phẩm";

            // 1. Khôi phục điều kiện tìm kiếm từ Session
            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";
            ViewBag.CategoryID = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_CategoryID") ?? 0;
            ViewBag.SupplierID = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_SupplierID") ?? 0;

            string minPriceStr = HttpContext.Session.GetString($"{SEARCH_CONDITION}_MinPrice");
            ViewBag.MinPrice = string.IsNullOrEmpty(minPriceStr) ? "" : minPriceStr;

            string maxPriceStr = HttpContext.Session.GetString($"{SEARCH_CONDITION}_MaxPrice");
            ViewBag.MaxPrice = string.IsNullOrEmpty(maxPriceStr) ? "" : maxPriceStr;

            // 2. Lấy danh sách Loại hàng và Nhà cung cấp cho Dropdown (ComboBox)
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            return View();
        }

        public async Task<IActionResult> Search(int page = 1, string searchValue = "", int categoryID = 0, int supplierID = 0, string minPrice = "", string maxPrice = "")
        {
            // 1. Lưu lại điều kiện tìm kiếm vào session (Bookmark)
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", searchValue ?? "");
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_CategoryID", categoryID);
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_SupplierID", supplierID);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_MinPrice", minPrice ?? "");
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_MaxPrice", maxPrice ?? "");

            // 2. Xử lý giá tiền (bỏ dấu phẩy)
            decimal min = 0;
            decimal max = 0;
            if (!string.IsNullOrWhiteSpace(minPrice)) decimal.TryParse(minPrice.Replace(",", ""), out min);
            if (!string.IsNullOrWhiteSpace(maxPrice)) decimal.TryParse(maxPrice.Replace(",", ""), out max);

            var input = new ProductSearchInput()
            {
                Page = page,
                PageSize = PAGESIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                SupplierID = supplierID,
                MinPrice = min,
                MaxPrice = max
            };

            // 3. Lấy dữ liệu và trả về PartialView (Bắt buộc cho Ajax)
            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView("Search", data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";

            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            var product = new Product()
            {
                ProductID = 0,
                IsSelling = true
            };
            return View("Edit", product);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật mặt hàng";

            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Product data, IFormFile? uploadPhoto)
        {
            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");

            if (!ModelState.IsValid)
            {
                var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
                var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

                ViewBag.Categories = categoryResult?.DataItems;
                ViewBag.Suppliers = supplierResult?.DataItems;
                return View("Edit", data);
            }

            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }

            if (string.IsNullOrEmpty(data.Photo))
                data.Photo = "nophoto.png";

            if (data.ProductID == 0)
            {
                long id = await CatalogDataService.AddProductAsync(data);
                if (id <= 0)
                {
                    ModelState.AddModelError("Error", "Tên mặt hàng bị trùng lặp");
                    var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
                    var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

                    ViewBag.Categories = categoryResult?.DataItems;
                    ViewBag.Suppliers = supplierResult?.DataItems;
                    return View("Edit", data);
                }
                return RedirectToAction("Edit", new { id = id });
            }
            else
            {
                bool result = await CatalogDataService.UpdateProductAsync(data);
                if (!result)
                {
                    ModelState.AddModelError("Error", "Cập nhật không thành công (có thể tên mặt hàng bị trùng)");
                    var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
                    var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

                    ViewBag.Categories = categoryResult?.DataItems;
                    ViewBag.Suppliers = supplierResult?.DataItems;
                    return View("Edit", data);
                }
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteProductAsync(id);
                return RedirectToAction("Index");
            }

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            return View(product);
        }

        // ==========================================
        // PHẦN 2: QUẢN LÝ THƯ VIỆN ẢNH (PRODUCT PHOTO)
        // ==========================================

        public async Task<IActionResult> Photo(int id = 0, string method = "", long photoId = 0)
        {
            switch (method)
            {
                case "add":
                    var newPhoto = new ProductPhoto()
                    {
                        PhotoID = 0,
                        ProductID = id,
                        IsHidden = false
                    };
                    return View("EditPhoto", newPhoto);

                case "edit":
                    var photo = await CatalogDataService.GetPhotoAsync(photoId);
                    if (photo == null) return RedirectToAction("Edit", new { id = id });
                    return View("EditPhoto", photo);

                case "delete":
                    await CatalogDataService.DeletePhotoAsync(photoId);
                    return RedirectToAction("Edit", new { id = id });

                default:
                    return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }

            if (string.IsNullOrEmpty(data.Photo))
                data.Photo = "nophoto.png";

            if (data.PhotoID == 0)
            {
                await CatalogDataService.AddPhotoAsync(data);
            }
            else
            {
                await CatalogDataService.UpdatePhotoAsync(data);
            }

            return RedirectToAction("Edit", new { id = data.ProductID });
        }


        // ==========================================
        // PHẦN 3: QUẢN LÝ THUỘC TÍNH (PRODUCT ATTRIBUTE)
        // ==========================================

        public async Task<IActionResult> Attribute(int id = 0, string method = "", long attributeId = 0)
        {
            switch (method)
            {
                case "add":
                    var newAttr = new ProductAttribute()
                    {
                        AttributeID = 0,
                        ProductID = id
                    };
                    return View("EditAttribute", newAttr);

                case "edit":
                    var attr = await CatalogDataService.GetAttributeAsync(attributeId);
                    if (attr == null) return RedirectToAction("Edit", new { id = id });
                    return View("EditAttribute", attr);

                case "delete":
                    await CatalogDataService.DeleteAttributeAsync(attributeId);
                    return RedirectToAction("Edit", new { id = id });

                default:
                    return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            if (data.AttributeID == 0)
            {
                await CatalogDataService.AddAttributeAsync(data);
            }
            else
            {
                await CatalogDataService.UpdateAttributeAsync(data);
            }

            return RedirectToAction("Edit", new { id = data.ProductID });
        }
    }
}