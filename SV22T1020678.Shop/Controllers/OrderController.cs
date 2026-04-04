using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020678.BusinessLayers;
using SV22T1020678.Models.Sales;
using System.Text.Json;
using System.Security.Claims;

namespace SV22T1020678.Shop.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới được thanh toán và xem lịch sử
    public class OrderController : Controller
    {
        // ==========================================
        // 1. CÁC HÀM XỬ LÝ THANH TOÁN (Của bạn)
        // ==========================================
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
            var cart = GetCart();
            // Lấy ID khách hàng từ tài khoản đang đăng nhập
            int customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // 1. Tạo đối tượng Order
            var orderData = new Order
            {
                CustomerID = customerId,
                OrderTime = DateTime.Now,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress,
                Status = (SV22T1020678.Models.Sales.OrderStatusEnum)1
            };

            // 2. Chuyển đổi giỏ hàng sang danh sách chi tiết đơn hàng
            var orderDetails = cart.Select(item => new OrderDetail
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.SalePrice
            });

            // 3. Lưu vào Database
            int orderId = await SalesDataService.SaveOrderAsync(orderData, orderDetails);

            if (orderId > 0)
            {
                HttpContext.Session.Remove("ShopCart"); // Xóa giỏ hàng sau khi đặt xong
                return Json(new { success = true, orderId = orderId });
            }
            return Json(new { success = false, message = "Lỗi khi tạo đơn hàng!" });
        }

        // ==========================================
        // 2. HÀM XEM LỊCH SỬ MUA HÀNG (Mới bổ sung)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> History()
        {
            // Tận dụng luôn cách lấy CustomerID cực chuẩn của bạn
            int customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Gọi hàm từ SalesDataService để lấy danh sách
            var data = await SalesDataService.ListOrdersByCustomerIdAsync(customerId);

            return View(data);
        }
    }
}