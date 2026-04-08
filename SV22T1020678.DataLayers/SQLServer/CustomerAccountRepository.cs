using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Security;
using System.Security.Cryptography;
using System.Text;

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

            // 1. Mã hóa mật khẩu khách nhập vào để so sánh với DB
            string md5Password = ToMD5(password);

            string sql = @"
                SELECT 
                    CAST(CustomerID AS VARCHAR) AS UserId,
                    Email AS UserName,
                    CustomerName AS DisplayName,
                    Email AS Email,
                    '' AS Photo,
                    'customer' AS RoleNames -- Khách hàng mặc định quyền là customer
                FROM Customers 
                WHERE Email = @userName 
                  AND Password = @password -- @password sẽ nhận giá trị md5Password
                  AND IsLocked = 0"; // Tài khoản không bị khóa

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
            {
                userName,
                password = md5Password
            });
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            // 2. Mã hóa mật khẩu mới trước khi lưu xuống Database
            string md5Password = ToMD5(password);

            string sql = @"
                UPDATE Customers 
                SET Password = @password 
                WHERE Email = @userName";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                userName,
                password = md5Password
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Hàm mã hóa chuỗi sang MD5 (Dùng chung chuẩn với Admin)
        /// </summary>
        private string ToMD5(string str)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(str);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}