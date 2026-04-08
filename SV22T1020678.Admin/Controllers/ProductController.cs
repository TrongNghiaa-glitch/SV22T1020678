using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting; // Thêm cái này để dùng IWebHostEnvironment
using SV22T1020678.Admin.AppCodes;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020678.Admin.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGESIZE = 20;
        private const string SEARCH_CONDITION = "ProductSearchCondition";

        // Khai báo để dùng cho việc lưu ảnh
        private readonly IWebHostEnvironment _hostEnvironment;

        // Hàm khởi tạo (Constructor) - Rất quan trọng để fix lỗi đỏ lúc nãy
        public ProductController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Quản lý Sản phẩm";

            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";
            ViewBag.CategoryID = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_CategoryID") ?? 0;
            ViewBag.SupplierID = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_SupplierID") ?? 0;

            string minPriceStr = HttpContext.Session.GetString($"{SEARCH_CONDITION}_MinPrice");
            ViewBag.MinPrice = string.IsNullOrEmpty(minPriceStr) ? "" : minPriceStr;

            string maxPriceStr = HttpContext.Session.GetString($"{SEARCH_CONDITION}_MaxPrice");
            ViewBag.MaxPrice = string.IsNullOrEmpty(maxPriceStr) ? "" : maxPriceStr;

            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            return View();
        }

        public async Task<IActionResult> Search(int page = 1, string searchValue = "", int categoryID = 0, int supplierID = 0, string minPrice = "", string maxPrice = "")
        {
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", searchValue ?? "");
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_CategoryID", categoryID);
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_SupplierID", supplierID);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_MinPrice", minPrice ?? "");
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_MaxPrice", maxPrice ?? "");

            decimal min = 0, max = 0;
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

            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView("Search", data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Thêm mặt hàng mới";
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            var product = new Product() { ProductID = 0, IsSelling = true, Photo = "nophoto.png" };
            return View("Edit", product);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin mặt hàng";
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Product data, IFormFile? uploadPhoto)
        {
            // 1. Validate
            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");
            if (data.CategoryID <= 0)
                ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

            if (!ModelState.IsValid)
            {
                var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
                var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });
                ViewBag.Categories = categoryResult?.DataItems;
                ViewBag.Suppliers = supplierResult?.DataItems;
                return View("Edit", data);
            }

            // 2. Xử lý ảnh chính của sản phẩm
            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, "images", "products", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }

            // 3. Lưu DB
            if (data.ProductID == 0)
            {
                await CatalogDataService.AddProductAsync(data);
                TempData["Message"] = $"Thêm mặt hàng {data.ProductName} thành công!";
            }
            else
            {
                await CatalogDataService.UpdateProductAsync(data);
                TempData["Message"] = $"Đã cập nhật thông tin cho {data.ProductName} thành công!";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteProductAsync(id);
                TempData["Message"] = "Đã xóa mặt hàng thành công!";
                return RedirectToAction("Index");
            }
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            return View(product);
        }

        // ==========================================
        // PHẦN 2 & 3: PHOTO & ATTRIBUTE (ĐÃ FIX PATH)
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            // 1. CHỐNG LỖI NULL: SQL Server cấm Description trống, mình gán tự động luôn!
            if (string.IsNullOrWhiteSpace(data.Description))
            {
                data.Description = "";
            }

            // 2. XỬ LÝ LƯU ẢNH VÀO THƯ MỤC
            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                // Dùng _hostEnvironment đã khai báo
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, "images", "products", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }

            // Đề phòng sếp quên chọn ảnh khi Thêm mới
            if (string.IsNullOrEmpty(data.Photo))
                data.Photo = "nophoto.png";

            // 3. LƯU VÀO DATABASE VÀ GỬI THÔNG BÁO POPUP
            if (data.PhotoID == 0)
            {
                await CatalogDataService.AddPhotoAsync(data);
                TempData["Message"] = "Bổ sung ảnh thành công!";
            }
            else
            {
                await CatalogDataService.UpdatePhotoAsync(data);
                TempData["Message"] = "Cập nhật ảnh thành công!";
            }

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        // Hàm điều hướng Photo (Đã sửa để gọi PartialView cho Popup)
        public async Task<IActionResult> Photo(int id = 0, string method = "", long photoId = 0)
        {
            switch (method.ToLower())
            {
                case "add":
                    // SỬA: Đổi View thành PartialView để trích xuất HTML sạch, không lấy thanh Menu
                    return PartialView("EditPhoto", new ProductPhoto { PhotoID = 0, ProductID = id, IsHidden = false });

                case "edit":
                    var photo = await CatalogDataService.GetPhotoAsync(photoId);
                    // SỬA: Đổi View thành PartialView
                    return photo == null ? RedirectToAction("Edit", new { id }) : PartialView("EditPhoto", photo);

                case "delete":
                    await CatalogDataService.DeletePhotoAsync(photoId);
                    // Gửi thông báo để lúc quay lại trang Edit nó "nổ" popup xóa thành công
                    TempData["Message"] = "Đã xóa ảnh thành công!";
                    return RedirectToAction("Edit", new { id });

                default:
                    return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Attribute(int id = 0, string method = "", long attributeId = 0)
        {
            switch (method.ToLower())
            {
                case "add":
                    return PartialView("EditAttribute", new ProductAttribute { AttributeID = 0, ProductID = id });
                case "edit":
                    var attr = await CatalogDataService.GetAttributeAsync(attributeId);
                    return attr == null ? RedirectToAction("Edit", new { id }) : PartialView("EditAttribute", attr);
                case "delete":
                    await CatalogDataService.DeleteAttributeAsync(attributeId);
                    TempData["Message"] = "Đã xóa thuộc tính thành công!";
                    return RedirectToAction("Edit", new { id });
                default: return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            if (data.AttributeID == 0)
            {
                await CatalogDataService.AddAttributeAsync(data);
                TempData["Message"] = "Bổ sung thuộc tính thành công!";
            }
            else
            {
                await CatalogDataService.UpdateAttributeAsync(data);
                TempData["Message"] = "Cập nhật thuộc tính thành công!";
            }
            return RedirectToAction("Edit", new { id = data.ProductID });
        }
    }
}