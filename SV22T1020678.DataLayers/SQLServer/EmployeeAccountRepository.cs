using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Security;
using System.Security.Cryptography;
using System.Text;

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

            // Mã hóa mật khẩu người dùng nhập vào để so sánh với DB
            string md5Password = ToMD5(password);

            string sql = @"
                SELECT 
                    CAST(EmployeeID AS VARCHAR) AS UserId,
                    Email AS UserName,
                    FullName AS DisplayName,
                    Email AS Email,
                    Photo AS Photo,
                    RoleNames AS RoleNames -- Lấy đúng quyền từ DB (admin, employee)
                FROM Employees 
                WHERE Email = @userName 
                  AND Password = @password -- @password ở đây sẽ nhận giá trị md5Password
                  AND IsWorking = 1";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
            {
                userName,
                password = md5Password
            });
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            // Phải mã hóa mật khẩu mới trước khi lưu xuống Database
            string md5Password = ToMD5(password);

            string sql = @"
                UPDATE Employees 
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
        /// Hàm mã hóa chuỗi sang MD5
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