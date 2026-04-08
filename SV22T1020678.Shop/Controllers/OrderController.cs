using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Sales;
using System.Text.Json;
using System.Security.Claims;

namespace SV22T1020678.Shop.Controllers
{
    [Authorize(AuthenticationSchemes = "CustomerWebAuth")]
    public class OrderController : Controller
    {
        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString("ShopCart");
            return string.IsNullOrEmpty(json) ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json)!;
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Init(string deliveryProvince, string deliveryAddress)
        {
            // 1. Kiểm tra giỏ hàng
            var cart = GetCart();
            if (cart.Count == 0) return Json(new { success = false, message = "Giỏ hàng của bạn đang trống!" });

            // 2. Lấy CustomerID an toàn (Tránh lỗi int.Parse nếu null)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int customerId))
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại!" });

            // 3. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(deliveryProvince) || string.IsNullOrEmpty(deliveryAddress))
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ địa chỉ giao hàng!" });

            try
            {
                // 4. Tạo đối tượng Order
                var orderData = new Order
                {
                    CustomerID = customerId,
                    OrderTime = DateTime.Now,
                    DeliveryProvince = deliveryProvince,
                    DeliveryAddress = deliveryAddress,
                    EmployeeID = 1, // Mặc định nhân viên hệ thống
                    Status = OrderStatusEnum.New
                };

                // 5. Chuyển đổi giỏ hàng (QUAN TRỌNG: Phải .ToList() để khớp kiểu dữ liệu)
                var orderDetails = cart.Select(item => new OrderDetail
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                }).ToList();

                // 6. Lưu vào Database
                int orderId = await SalesDataService.SaveOrderAsync(orderData, orderDetails);

                if (orderId > 0)
                {
                    HttpContext.Session.Remove("ShopCart"); // Xóa giỏ hàng
                    return Json(new { success = true, orderId = orderId });
                }

                return Json(new { success = false, message = "Hệ thống không thể lưu đơn hàng lúc này." });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi code bên trong (ví dụ DB lỗi), nó sẽ báo về đây thay vì đứng im
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int customerId)) return RedirectToAction("Login", "Account");

            var data = await SalesDataService.ListOrdersByCustomerIdAsync(customerId);
            return View(data);
        }
    }
}