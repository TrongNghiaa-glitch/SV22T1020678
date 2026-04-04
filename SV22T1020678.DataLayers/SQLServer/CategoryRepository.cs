using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models; // Thay đổi namespace này nếu model Category của bạn nằm ở thư mục khác
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using System.Data;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho danh mục loại hàng (Category) trên CSDL SQL Server
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo lớp repository với chuỗi kết nối
        /// </summary>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Categories (CategoryName, Description)
                VALUES (@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { CategoryID = id });
            return rowsAffected > 0;
        }

        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Categories WHERE CategoryID = @CategoryID";
            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Kiểm tra xem Loại hàng này đã có mặt hàng (Product) nào thuộc về nó chưa
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Products WHERE CategoryID = @CategoryID) THEN 1 
                    ELSE 0 
                END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { CategoryID = id });
        }

        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchCondition = $"%{input.SearchValue}%";

            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Categories 
                WHERE (@SearchValue = N'%%') 
                   OR (CategoryName LIKE @SearchValue) 
                   OR (Description LIKE @SearchValue)";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchCondition });

            var result = new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Category>()
            };

            if (rowCount == 0) return result;

            string sqlData;
            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT * FROM Categories 
                    WHERE (@SearchValue = N'%%') 
                       OR (CategoryName LIKE @SearchValue) 
                       OR (Description LIKE @SearchValue)
                    ORDER BY CategoryName";
                var data = await connection.QueryAsync<Category>(sqlData, new { SearchValue = searchCondition });
                result.DataItems = data.ToList();
            }
            else
            {
                sqlData = @"
                    SELECT * FROM Categories 
                    WHERE (@SearchValue = N'%%') 
                       OR (CategoryName LIKE @SearchValue) 
                       OR (Description LIKE @SearchValue)
                    ORDER BY CategoryName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                var data = await connection.QueryAsync<Category>(sqlData, new
                {
                    SearchValue = searchCondition,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data.ToList();
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Categories
                SET CategoryName = @CategoryName,
                    Description = @Description
                WHERE CategoryID = @CategoryID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }
    }
}