using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Security;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Xử lý dữ liệu liên quan đến tài khoản của nhân viên trên SQL Server
    /// </summary>
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT 
                    CAST(EmployeeID AS VARCHAR) AS UserId,
                    Email AS UserName,
                    FullName AS DisplayName,
                    Email AS Email,
                    Photo AS Photo,
                    'Employee' AS RoleNames
                FROM Employees 
                WHERE Email = @userName AND Password = @password AND IsWorking = 1";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Employees 
                SET Password = @password 
                WHERE Email = @userName";

            int rowsAffected = await connection.ExecuteAsync(sql, new { userName, password });
            return rowsAffected > 0;
        }
    }
}