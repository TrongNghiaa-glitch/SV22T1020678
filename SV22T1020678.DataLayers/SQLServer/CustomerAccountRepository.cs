using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Security;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Xử lý dữ liệu liên quan đến tài khoản của khách hàng trên SQL Server
    /// </summary>
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT 
                    CAST(CustomerID AS VARCHAR) AS UserId,
                    Email AS UserName,
                    CustomerName AS DisplayName,
                    Email AS Email,
                    '' AS Photo,
                    'Customer' AS RoleNames
                FROM Customers 
                WHERE Email = @userName AND Password = @password AND IsLocked = 0";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Customers 
                SET Password = @password 
                WHERE Email = @userName";

            int rowsAffected = await connection.ExecuteAsync(sql, new { userName, password });
            return rowsAffected > 0;
        }
    }
}