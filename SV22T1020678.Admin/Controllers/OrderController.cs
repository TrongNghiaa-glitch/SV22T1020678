using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Sales;
using System;
using System.Collections.Generic;
using System.Globalization; // 🌟 ĐÃ THÊM: Thư viện để ép kiểu ngày tháng
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SV22T1020678.Admin.Controllers
{
    public class OrderController : Controller
    {
        private const int PAGESIZE = 20;
        private const string SEARCH_CONDITION = "OrderSearchCondition";
        private const string CART_SESSION = "ShoppingCart";

        // ==========================================
        // 1. TÌM KIẾM & DANH SÁCH ĐƠN HÀNG
        // ==========================================
        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Đơn hàng";
            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";
            ViewBag.Status = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Status") ?? 0;

            // 🌟 ĐÃ THÊM: Lấy khoảng thời gian từ Session ra lại
            ViewBag.TimeRange = HttpContext.Session.GetString($"{SEARCH_CONDITION}_TimeRange") ?? "";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(OrderSearchInput condition, string TimeRange = "")
        {
            // 1. Lưu Session
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", condition.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", condition.SearchValue ?? "");
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Status", (int)condition.Status);

            // 🌟 ĐÃ THÊM: Lưu khoảng thời gian vào Session
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_TimeRange", TimeRange ?? "");

            // 2. KỸ THUẬT "CHẶT" CHUỖI TÌM KIẾM NGÀY THÁNG
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrWhiteSpace(TimeRange))
            {
                var times = TimeRange.Split('-'); // Cắt chuỗi bởi dấu gạch ngang
                if (times.Length == 2)
                {
                    // Ép kiểu chuỗi "dd/MM/yyyy" thành DateTime
                    if (DateTime.TryParseExact(times[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime f))
                    {
                        fromDate = f;
                    }
                    if (DateTime.TryParseExact(times[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t))
                    {
                        // Cộng thêm 1 ngày rồi trừ đi 1 tick để lấy đến 23:59:59 của ngày đó
                        toDate = t.AddDays(1).AddTicks(-1);
                    }
                }
            }

            // Gán dữ liệu ngày tháng vào condition
            condition.DateFrom = fromDate;
            condition.DateTo = toDate;
            condition.PageSize = PAGESIZE;

            var data = await SalesDataService.ListOrdersAsync(condition);
            return PartialView("Search", data);
        }

        // ==========================================
        // 2. LẬP ĐƠN HÀNG & QUẢN LÝ GIỎ HÀNG
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Lập đơn hàng";
            var input = new PaginationSearchInput { Page = 1, PageSize = 1000, SearchValue = "" };

            var customersResult = await PartnerDataService.ListCustomersAsync(input);
            ViewBag.Customers = customersResult.DataItems;
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();

            var categoryInput = new PaginationSearchInput { Page = 1, PageSize = 1000, SearchValue = "" };
            ViewBag.Categories = (await CatalogDataService.ListCategoriesAsync(categoryInput)).DataItems;

            var cart = GetCart();
            return View(cart);
        }

        [HttpGet]
        public async Task<IActionResult> SearchProduct(int categoryID = 0, int page = 1, string searchValue = "")
        {
            var input = new ProductSearchInput
            {
                Page = page,
                PageSize = PAGESIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID
            };

            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView("SearchProduct", data);
        }

        [HttpPost]
        public IActionResult AddToCart(CartItem item)
        {
            if (item.Quantity <= 0 || item.SalePrice < 0) return RedirectToAction("Create");
            var cart = GetCart();
            var existsItem = cart.FirstOrDefault(m => m.ProductID == item.ProductID);
            if (existsItem == null)
            {
                cart.Add(item);
            }
            else
            {
                existsItem.Quantity += item.Quantity;
                existsItem.SalePrice = item.SalePrice;
            }
            SaveCart(cart);
            return RedirectToAction("Create");
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Create");
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CART_SESSION);
            return RedirectToAction("Create");
        }

        [HttpPost]
        public async Task<IActionResult> Init(int customerID, string deliveryProvince, string deliveryAddress)
        {
            var cart = GetCart();
            if (cart.Count == 0) return Json(new { success = false, message = "Giỏ hàng đang trống!" });
            if (customerID <= 0 || string.IsNullOrEmpty(deliveryProvince) || string.IsNullOrEmpty(deliveryAddress))
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin khách hàng, tỉnh thành và địa chỉ!" });

            var order = new Order
            {
                CustomerID = customerID,
                OrderTime = DateTime.Now,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress,
                EmployeeID = 1,
                Status = (OrderStatusEnum)1
            };

            var details = cart.Select(c => new OrderDetail
            {
                ProductID = c.ProductID,
                Quantity = c.Quantity,
                SalePrice = c.SalePrice
            }).ToList();

            int orderId = await SalesDataService.SaveOrderAsync(order, details);
            if (orderId > 0)
            {
                HttpContext.Session.Remove(CART_SESSION);
                return Json(new { success = true, orderID = orderId });
            }

            return Json(new { success = false, message = "Không thể lưu đơn hàng vào hệ thống." });
        }

        // ==========================================
        // 3. CHI TIẾT & CÁC THAO TÁC ĐƠN HÀNG
        // ==========================================
        public async Task<IActionResult> Detail(int id = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("Index");
            ViewBag.OrderDetails = await SalesDataService.ListOrderDetailsAsync(id);
            return View(order);
        }

        [HttpGet] public IActionResult Accept(int id = 0) => View(id);

        [HttpPost]
        public async Task<IActionResult> Accept(int id, string dummy = "")
        {
            await SalesDataService.AcceptOrderAsync(id);
            return Json(new { success = true, message = "Đã duyệt và chấp nhận đơn hàng thành công!" });
        }

        [HttpGet] public IActionResult Cancel(int id = 0) => View(id);

        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string dummy = "")
        {
            await SalesDataService.CancelOrderAsync(id);
            return RedirectToAction("Detail", new { id = id });
        }

        [HttpGet] public IActionResult Reject(int id = 0) => View(id);

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason = "")
        {
            await SalesDataService.RejectOrderAsync(id);
            return RedirectToAction("Detail", new { id = id });
        }

        [HttpGet] public IActionResult Delete(int id = 0) => View(id);

        [HttpPost]
        public async Task<IActionResult> Delete(int id, string dummy = "")
        {
            await SalesDataService.DeleteOrderAsync(id);
            return Json(new { success = true, message = "Đã xóa đơn hàng thành công!" });
        }

        [HttpGet] public IActionResult Finish(int id = 0) => View(id);

        [HttpPost]
        public async Task<IActionResult> Finish(int id, string dummy = "")
        {
            await SalesDataService.FinishOrderAsync(id);
            return Json(new { success = true, message = "Đã xác nhận hoàn tất đơn hàng thành công!" });
        }

        [HttpGet]
        public async Task<IActionResult> Shipping(int id = 0)
        {
            ViewBag.OrderID = id;
            var input = new PaginationSearchInput { Page = 1, PageSize = 1000, SearchValue = "" };
            var shippersResult = await PartnerDataService.ListShippersAsync(input);
            ViewBag.Shippers = shippersResult.DataItems;
            return View(id);
        }

        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            if (shipperID <= 0) return RedirectToAction("Detail", new { id = id });
            await SalesDataService.ShipOrderAsync(id, shipperID);
            return RedirectToAction("Detail", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShippingAddress(int id, string deliveryProvince, string deliveryAddress)
        {
            if (id <= 0 || string.IsNullOrEmpty(deliveryProvince) || string.IsNullOrEmpty(deliveryAddress))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin Tỉnh/Thành và Địa chỉ!" });
            }

            var currentOrder = await SalesDataService.GetOrderAsync(id);
            if (currentOrder == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            var orderToUpdate = new Order
            {
                OrderID = currentOrder.OrderID,
                CustomerID = currentOrder.CustomerID,
                OrderTime = currentOrder.OrderTime,
                EmployeeID = currentOrder.EmployeeID,
                AcceptTime = currentOrder.AcceptTime,
                ShipperID = currentOrder.ShipperID,
                ShippedTime = currentOrder.ShippedTime,
                FinishedTime = currentOrder.FinishedTime,
                Status = currentOrder.Status,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress
            };

            bool result = await SalesDataService.UpdateOrderAsync(orderToUpdate);

            if (result)
            {
                return Json(new { success = true, message = "Đã cập nhật thông tin giao hàng thành công!" });
            }

            return Json(new { success = false, message = "Không thể cập nhật thông tin lúc này." });
        }

        // ==========================================
        // 4. HÀM BỔ TRỢ QUẢN LÝ SESSION
        // ==========================================
        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CART_SESSION);
            if (string.IsNullOrEmpty(json)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CART_SESSION, JsonSerializer.Serialize(cart));
        }
    }
}