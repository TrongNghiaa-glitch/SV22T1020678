using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.HR;
using System.Data;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhân viên (Employee) trên CSDL SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo lớp repository với chuỗi kết nối
        /// </summary>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Employees (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";

            int rowsAffected = await connection.ExecuteAsync(sql, new { EmployeeID = id });
            return rowsAffected > 0;
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";

            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            // Kiểm tra xem nhân viên đã từng lập đơn hàng nào chưa
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID) THEN 1 
                    ELSE 0 
                END";

            return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            string searchCondition = $"%{input.SearchValue}%";

            // Đếm tổng số dòng (Tìm kiếm theo tên, điện thoại hoặc email)
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Employees 
                WHERE (@SearchValue = N'%%') 
                   OR (FullName LIKE @SearchValue) 
                   OR (Phone LIKE @SearchValue)
                   OR (Email LIKE @SearchValue)";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchCondition });

            var result = new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Employee>()
            };

            if (rowCount == 0) return result;

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT * FROM Employees 
                    WHERE (@SearchValue = N'%%') 
                       OR (FullName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                    ORDER BY FullName";

                var data = await connection.QueryAsync<Employee>(sqlData, new { SearchValue = searchCondition });
                result.DataItems = data.ToList();
            }
            else
            {
                sqlData = @"
                    SELECT * FROM Employees 
                    WHERE (@SearchValue = N'%%') 
                       OR (FullName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                    ORDER BY FullName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Employee>(sqlData, new
                {
                    SearchValue = searchCondition,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data.ToList();
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Employees
                SET FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking
                WHERE EmployeeID = @EmployeeID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT COUNT(*) 
                FROM Employees 
                WHERE Email = @Email AND EmployeeID <> @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, id = id });

            // Trả về true nếu không tìm thấy nhân viên nào khác đang dùng chung Email này
            return count == 0;
        }
    }
}