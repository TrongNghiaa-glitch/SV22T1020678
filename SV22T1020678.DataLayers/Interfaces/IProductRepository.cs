using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;

namespace SV22T1020678.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu cho mặt hàng
    /// </summary>
    public interface IProductRepository
    {
        #region Xử lý mặt hàng (Products)
        /// <summary>
        /// Tìm kiếm và lấy danh sách mặt hàng dưới dạng phân trang
        /// </summary>
        Task<PagedResult<Product>> ListAsync(ProductSearchInput input);

        /// <summary>
        /// Lấy thông tin 1 mặt hàng theo ID
        /// </summary>
        Task<Product?> GetAsync(int productID);

        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        Task<int> AddAsync(Product data);

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        Task<bool> UpdateAsync(Product data);

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        Task<bool> DeleteAsync(int productID);

        /// <summary>
        /// Kiểm tra xem mặt hàng có đang được sử dụng trong chi tiết đơn hàng hay không
        /// </summary>
        Task<bool> IsUsedAsync(int productID);
        #endregion

        #region Xử lý Thuộc tính của mặt hàng (Product Attributes)
        Task<List<ProductAttribute>> ListAttributesAsync(int productID);
        Task<ProductAttribute?> GetAttributeAsync(long attributeID);
        Task<long> AddAttributeAsync(ProductAttribute data);
        Task<bool> UpdateAttributeAsync(ProductAttribute data);
        Task<bool> DeleteAttributeAsync(long attributeID);
        #endregion

        #region Xử lý Ảnh của mặt hàng (Product Photos)
        Task<List<ProductPhoto>> ListPhotosAsync(int productID);
        Task<ProductPhoto?> GetPhotoAsync(long photoID);
        Task<long> AddPhotoAsync(ProductPhoto data);
        Task<bool> UpdatePhotoAsync(ProductPhoto data);
        Task<bool> DeletePhotoAsync(long photoID);
        #endregion
    }
}