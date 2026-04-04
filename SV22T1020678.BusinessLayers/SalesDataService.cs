using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper; // Thêm Dapper để truy vấn nhanh
using Microsoft.Data.SqlClient; // Thêm SqlClient để mở kết nối
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.DataLayers.SQLServer;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Sales;

namespace SV22T1020678.BusinessLayers
{
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Đơn hàng (Orders)
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input) => await orderDB.ListAsync(input);

        public static async Task<OrderViewInfo?> GetOrderAsync(int id) => await orderDB.GetAsync(id);

        public static async Task<int> AddOrderAsync(Order data) => await orderDB.AddAsync(data);

        public static async Task<bool> UpdateOrderAsync(Order data) => await orderDB.UpdateAsync(data);

        public static async Task<bool> DeleteOrderAsync(int id) => await orderDB.DeleteAsync(id);

        // =========================================================
        // BỔ SUNG CÁC HÀM XỬ LÝ TRẠNG THÁI ĐƠN HÀNG
        // =========================================================
        public static async Task<bool> AcceptOrderAsync(int id) => await orderDB.AcceptAsync(id);

        public static async Task<bool> ShipOrderAsync(int id, int shipperID) => await orderDB.ShipAsync(id, shipperID);

        public static async Task<bool> FinishOrderAsync(int id) => await orderDB.FinishAsync(id);

        public static async Task<bool> CancelOrderAsync(int id) => await orderDB.CancelAsync(id);

        public static async Task<bool> RejectOrderAsync(int id) => await orderDB.RejectAsync(id);

        /// <summary>
        /// Lấy danh sách đơn hàng theo ID của Khách hàng (Dành riêng cho Shop)
        /// </summary>
        public static async Task<List<Order>> ListOrdersByCustomerIdAsync(int customerId)
        {
            using var connection = new SqlConnection(Configuration.ConnectionString);
            string sql = "SELECT * FROM Orders WHERE CustomerID = @CustomerID ORDER BY OrderTime DESC";
            var data = await connection.QueryAsync<Order>(sql, new { CustomerID = customerId });
            return data.ToList();
        }
        #endregion

        #region Chi tiết Đơn hàng (OrderDetails)
        public static async Task<List<OrderDetailViewInfo>> ListOrderDetailsAsync(int orderId) => await orderDB.ListDetailsAsync(orderId);

        public static async Task<OrderDetailViewInfo?> GetOrderDetailAsync(int orderId, int productId) => await orderDB.GetDetailAsync(orderId, productId);

        public static async Task<bool> AddOrderDetailAsync(OrderDetail data) => await orderDB.AddDetailAsync(data);

        public static async Task<bool> UpdateOrderDetailAsync(OrderDetail data) => await orderDB.UpdateDetailAsync(data);

        public static async Task<bool> DeleteOrderDetailAsync(int orderId, int productId) => await orderDB.DeleteDetailAsync(orderId, productId);

        /// <summary>
        /// Lưu đơn hàng và chi tiết đơn hàng
        /// </summary>
        public static async Task<int> SaveOrderAsync(Order data, IEnumerable<OrderDetail> details)
        {
            return await orderDB.SaveOrderAsync(data, details);
        }

        #endregion
    }
}