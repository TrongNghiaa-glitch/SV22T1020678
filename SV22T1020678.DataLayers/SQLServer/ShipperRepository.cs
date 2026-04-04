using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models; // Thay đổi namespace này nếu model Shipper của bạn nằm ở thư mục khác
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System.Data;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho người giao hàng (Shipper) trên CSDL SQL Server
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo lớp repository với chuỗi kết nối
        /// </summary>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Shippers (ShipperName, Phone)
                VALUES (@ShipperName, @Phone);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Shippers WHERE ShipperID = @ShipperID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { ShipperID = id });
            return rowsAffected > 0;
        }

        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Shippers WHERE ShipperID = @ShipperID";
            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Kiểm tra xem Shipper này đã từng giao đơn hàng nào trong bảng Orders chưa
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Orders WHERE ShipperID = @ShipperID) THEN 1 
                    ELSE 0 
                END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { ShipperID = id });
        }

        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchCondition = $"%{input.SearchValue}%";

            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Shippers 
                WHERE (@SearchValue = N'%%') 
                   OR (ShipperName LIKE @SearchValue) 
                   OR (Phone LIKE @SearchValue)";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchCondition });

            var result = new PagedResult<Shipper>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Shipper>()
            };

            if (rowCount == 0) return result;

            string sqlData;
            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT * FROM Shippers 
                    WHERE (@SearchValue = N'%%') 
                       OR (ShipperName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                    ORDER BY ShipperName";
                var data = await connection.QueryAsync<Shipper>(sqlData, new { SearchValue = searchCondition });
                result.DataItems = data.ToList();
            }
            else
            {
                sqlData = @"
                    SELECT * FROM Shippers 
                    WHERE (@SearchValue = N'%%') 
                       OR (ShipperName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                var data = await connection.QueryAsync<Shipper>(sqlData, new
                {
                    SearchValue = searchCondition,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data.ToList();
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Shippers
                SET ShipperName = @ShipperName,
                    Phone = @Phone
                WHERE ShipperID = @ShipperID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }
    }
}