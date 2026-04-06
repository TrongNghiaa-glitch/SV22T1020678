using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using System.Data;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho mặt hàng (Product), thuộc tính và ảnh của mặt hàng
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Xử lý dữ liệu bảng Products

        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Products (ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                VALUES (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Products WHERE ProductID = @ProductID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { ProductID = productID });
            return rowsAffected > 0;
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Products WHERE ProductID = @ProductID";
            return await connection.QueryFirstOrDefaultAsync<Product?>(sql, new { ProductID = productID });
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            PagedResult<Product> result = new PagedResult<Product>();
            using var connection = new SqlConnection(_connectionString);

            string searchCondition = $"%{input.SearchValue}%";

            // --- ĐÃ BỔ SUNG: Xử lý logic sắp xếp (SortOrder) ---
            string orderClause = "ProductName ASC"; // Mặc định sắp xếp theo Tên A-Z
            if (!string.IsNullOrEmpty(input.SortOrder))
            {
                if (input.SortOrder == "PriceASC") orderClause = "Price ASC";
                else if (input.SortOrder == "PriceDESC") orderClause = "Price DESC";
                else if (input.SortOrder == "NameDESC") orderClause = "ProductName DESC";
            }

            // 1. Đếm tổng số dòng (Giữ nguyên)
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Products 
                WHERE (@SearchValue = N'%%' OR ProductName LIKE @SearchValue)
                  AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                  AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                  AND (Price >= @MinPrice)
                  AND (@MaxPrice = 0 OR Price <= @MaxPrice)";

            result.RowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new
            {
                SearchValue = searchCondition,
                CategoryID = input.CategoryID,
                SupplierID = input.SupplierID,
                MinPrice = input.MinPrice,
                MaxPrice = input.MaxPrice
            });

            // 2. Lấy dữ liệu (Đã đổi sang $@ để chèn biến orderClause vào SQL)
            string sqlData = "";
            if (input.PageSize == 0)
            {
                sqlData = $@"
                    SELECT * FROM Products 
                    WHERE (@SearchValue = N'%%' OR ProductName LIKE @SearchValue)
                      AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                      AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                      AND (Price >= @MinPrice)
                      AND (@MaxPrice = 0 OR Price <= @MaxPrice)
                    ORDER BY {orderClause}"; // <--- Đã sửa dòng này
                var data = await connection.QueryAsync<Product>(sqlData, new
                {
                    SearchValue = searchCondition,
                    CategoryID = input.CategoryID,
                    SupplierID = input.SupplierID,
                    MinPrice = input.MinPrice,
                    MaxPrice = input.MaxPrice
                });
                result.DataItems = data.ToList();
            }
            else
            {
                sqlData = $@"
                    SELECT * FROM Products 
                    WHERE (@SearchValue = N'%%' OR ProductName LIKE @SearchValue)
                      AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                      AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                      AND (Price >= @MinPrice)
                      AND (@MaxPrice = 0 OR Price <= @MaxPrice)
                    ORDER BY {orderClause} 
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"; // <--- Đã sửa dòng này
                var data = await connection.QueryAsync<Product>(sqlData, new
                {
                    SearchValue = searchCondition,
                    CategoryID = input.CategoryID,
                    SupplierID = input.SupplierID,
                    MinPrice = input.MinPrice,
                    MaxPrice = input.MaxPrice,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data.ToList();
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Products
                SET ProductName = @ProductName,
                    ProductDescription = @ProductDescription,
                    SupplierID = @SupplierID,
                    CategoryID = @CategoryID,
                    Unit = @Unit,
                    Price = @Price,
                    Photo = @Photo,
                    IsSelling = @IsSelling
                WHERE ProductID = @ProductID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        // Đã sửa: Bổ sung logic kiểm tra xem Sản phẩm có đơn hàng nào không
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE WHEN EXISTS(SELECT * FROM OrderDetails WHERE ProductID = @ProductID) 
                THEN 1 ELSE 0 END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { ProductID = productID });
        }

        #endregion

        #region Xử lý dữ liệu bảng ProductAttributes

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue, DisplayOrder)
                VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { AttributeID = attributeID });
            return rowsAffected > 0;
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
        }

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductAttributes WHERE ProductID = @ProductID ORDER BY DisplayOrder";
            var data = await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID });
            return data.ToList();
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductAttributes
                SET ProductID = @ProductID,
                    AttributeName = @AttributeName,
                    AttributeValue = @AttributeValue,
                    DisplayOrder = @DisplayOrder
                WHERE AttributeID = @AttributeID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        #endregion

        #region Xử lý dữ liệu bảng ProductPhotos

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductPhotos (ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { PhotoID = photoID });
            return rowsAffected > 0;
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
        }

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductPhotos WHERE ProductID = @ProductID ORDER BY DisplayOrder";
            var data = await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID });
            return data.ToList();
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductPhotos
                SET ProductID = @ProductID,
                    Photo = @Photo,
                    Description = @Description,
                    DisplayOrder = @DisplayOrder,
                    IsHidden = @IsHidden
                WHERE PhotoID = @PhotoID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        #endregion
    }
}