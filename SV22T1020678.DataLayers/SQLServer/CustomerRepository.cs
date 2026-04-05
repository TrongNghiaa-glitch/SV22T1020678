using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020678.DataLayers.SQLServer
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            // ĐÃ BỔ SUNG CỘT PASSWORD
            string sql = @"
                INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Customers WHERE CustomerID = @CustomerID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { CustomerID = id });
            return rowsAffected > 0;
        }

        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"SELECT * FROM Customers WHERE CustomerID = @id";
            var parameters = new { id = id };
            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, parameters);
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Orders WHERE CustomerID = @CustomerID) THEN 1 
                    ELSE 0 
                END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { CustomerID = id });
        }

        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchCondition = $"%{input.SearchValue}%";
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Customers 
                WHERE (@SearchValue = N'%%') 
                   OR (CustomerName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue) 
                   OR (Phone LIKE @SearchValue)
                   OR (Email LIKE @SearchValue)";
            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchCondition });

            var result = new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Customer>()
            };

            if (rowCount == 0) return result;

            string sqlData;
            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT * FROM Customers 
                    WHERE (@SearchValue = N'%%') 
                       OR (CustomerName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                    ORDER BY CustomerName";
                var data = await connection.QueryAsync<Customer>(sqlData, new { SearchValue = searchCondition });
                result.DataItems = data?.ToList() ?? new List<Customer>();
            }
            else
            {
                sqlData = @"
                    SELECT * FROM Customers 
                    WHERE (@SearchValue = N'%%') 
                       OR (CustomerName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                var data = await connection.QueryAsync<Customer>(sqlData, new
                {
                    SearchValue = searchCondition,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data?.ToList() ?? new List<Customer>();
            }
            return result;
        }

        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            // ĐÃ BỔ SUNG CẬP NHẬT CỘT PASSWORD
            string sql = @"
                UPDATE Customers
                SET CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Password = @Password,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        public async Task<bool> IsValidEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*) 
                FROM Customers 
                WHERE Email = @Email AND CustomerID <> @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, id = id });
            return count == 0;
        }

        public async Task<List<string>> GetProvincesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";
            var data = await connection.QueryAsync<string>(sql);
            return data.ToList();
        }
    }
}