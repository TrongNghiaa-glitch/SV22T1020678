using Microsoft.AspNetCore.Mvc;
using SV22T1020678.Models.Sales;
using System.Text.Json;

namespace SV22T1020678.Shop.Controllers
{
    public class CartController : Controller
    {
        private const string SHOP_CART_SESSION = "ShopCart";

        // 1. Hàm hỗ trợ Lấy giỏ hàng từ Session
        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(SHOP_CART_SESSION);
            if (string.IsNullOrEmpty(json)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        // 2. Hàm hỗ trợ Lưu giỏ hàng vào Session
        private void SaveCart(List<CartItem> cart)
        {
            var json = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(SHOP_CART_SESSION, json);
        }

        // 3. Hiển thị trang Giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // 4. Thêm sản phẩm vào giỏ
        [HttpPost]
        public IActionResult Add(int productId, string productName, decimal salePrice, int quantity = 1)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductID == productId);

            if (item != null)
            {
                item.Quantity += quantity; // Nếu có rồi thì tăng số lượng
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductID = productId,
                    ProductName = productName,
                    SalePrice = salePrice,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            return RedirectToAction("Index"); // Thêm xong chuyển sang trang giỏ hàng
        }

        // 5. Xóa mặt hàng khỏi giỏ
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductID == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // 6. Xóa sạch giỏ hàng
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(SHOP_CART_SESSION);
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            // Lấy giỏ hàng hiện tại ra
            var json = HttpContext.Session.GetString("ShopCart");
            var cart = string.IsNullOrEmpty(json) ? new List<SV22T1020678.Models.Sales.CartItem>() : System.Text.Json.JsonSerializer.Deserialize<List<SV22T1020678.Models.Sales.CartItem>>(json);

            // Tìm mặt hàng cần đổi số lượng
            var item = cart?.FirstOrDefault(c => c.ProductID == productId);
            if (item != null)
            {
                // Cập nhật số lượng mới (nếu nhập < 1 thì mặc định cho bằng 1)
                item.Quantity = quantity > 0 ? quantity : 1;
            }

            // Lưu ngược lại vào Session
            HttpContext.Session.SetString("ShopCart", System.Text.Json.JsonSerializer.Serialize(cart));

            // Tải lại trang Giỏ hàng
            return RedirectToAction("Index");
        }
    }
}