using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020678.Models.Sales
{
    /// <summary>
    /// Mặt hàng trong giỏ hàng
    /// </summary>
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Photo { get; set; } = "";
        public int Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TotalPrice => Quantity * SalePrice;
    }
}