using SV22T1020678.Models.Common;
using SV22T1020678.Models.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020678.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các chức năng xử lý dữ liệu cho đơn hàng
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input);

        /// <summary>
        /// Lấy thông tin 1 đơn hàng
        /// </summary>
        Task<OrderViewInfo?> GetAsync(int orderID);

        /// <summary>
        /// Bổ sung đơn hàng
        /// </summary>
        Task<int> AddAsync(Order data);

        /// <summary>
        /// Cập nhật đơn hàng
        /// </summary>
        Task<bool> UpdateAsync(Order data);

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        Task<bool> DeleteAsync(int orderID);

        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID);

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong một đơn hàng
        /// </summary>
        Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID);

        /// <summary>
        /// Bổ sung mặt hàng vào đơn hàng
        /// </summary>
        Task<bool> AddDetailAsync(OrderDetail data);

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng
        /// </summary>
        Task<bool> UpdateDetailAsync(OrderDetail data);

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        Task<bool> DeleteDetailAsync(int orderID, int productID);

        // ==========================================
        // CÁC HÀM XỬ LÝ TRẠNG THÁI ĐƠN HÀNG
        // ==========================================

        /// <summary>Duyệt đơn hàng</summary>
        Task<bool> AcceptAsync(int orderID);

        /// <summary>Chuyển giao hàng</summary>
        Task<bool> ShipAsync(int orderID, int shipperID);

        /// <summary>Xác nhận hoàn tất đơn hàng</summary>
        Task<bool> FinishAsync(int orderID);

        /// <summary>Hủy đơn hàng</summary>
        Task<bool> CancelAsync(int orderID);

        /// <summary>Từ chối đơn hàng</summary>
        Task<bool> RejectAsync(int orderID);

        /// <summary>
        /// Lưu đơn hàng và chi tiết đơn hàng (Dùng transaction để đảm bảo toàn vẹn dữ liệu)
        /// </summary>
        Task<int> SaveOrderAsync(Order data, IEnumerable<OrderDetail> details);
    }
}