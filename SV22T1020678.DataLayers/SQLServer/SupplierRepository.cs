using Dapper;
using SV22T1020678.DataLayers.Interfaces;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System.Data;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp (Supplier) trên CSDL SQL Server
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo lớp repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần bổ sung</param>
        /// <returns>Mã SupplierID (IDENTITY) của nhà cung cấp vừa được tạo</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Suppliers (SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Xóa nhà cung cấp dựa vào mã nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp (SupplierID) cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại trả về False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";

            int rowsAffected = await connection.ExecuteAsync(sql, new { SupplierID = id });
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần lấy (SupplierID)</param>
        /// <returns>Đối tượng Supplier nếu tìm thấy, ngược lại trả về null</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = "SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có dữ liệu liên quan (trong bảng Products) hay không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần kiểm tra</param>
        /// <returns>True nếu nhà cung cấp đã có mặt hàng liên quan, ngược lại là False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Products WHERE SupplierID = @SupplierID) THEN 1 
                    ELSE 0 
                END";

            return await connection.ExecuteScalarAsync<bool>(sql, new { SupplierID = id });
        }

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách nhà cung cấp
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm, phân trang (trang số mấy, kích thước trang, từ khóa...)</param>
        /// <returns>Danh sách nhà cung cấp đã được phân trang (đối tượng PagedResult)</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            string searchCondition = $"%{input.SearchValue}%";

            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Suppliers 
                WHERE (@SearchValue = N'%%') 
                   OR (SupplierName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue) 
                   OR (Phone LIKE @SearchValue)";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchCondition });

            var result = new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Supplier>()
            };

            if (rowCount == 0) return result;

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT * FROM Suppliers 
                    WHERE (@SearchValue = N'%%') 
                       OR (SupplierName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                    ORDER BY SupplierName";

                var data = await connection.QueryAsync<Supplier>(sqlData, new { SearchValue = searchCondition });
                result.DataItems = data.ToList();
            }
            else
            {
                sqlData = @"
                    SELECT * FROM Suppliers 
                    WHERE (@SearchValue = N'%%') 
                       OR (SupplierName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Supplier>(sqlData, new
                {
                    SearchValue = searchCondition,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data.ToList();
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp
        /// </summary>
        /// <param name="data">Dữ liệu mới của nhà cung cấp</param>
        /// <returns>True nếu cập nhật thành công, ngược lại trả về False</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Suppliers
                SET SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }
    }
}