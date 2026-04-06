using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.DataDictionary;
using System.Data;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt phép xử lý dữ liệu cho Tỉnh/Thành trên CSDL SQL Server
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo lớp repository với chuỗi kết nối
        /// </summary>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách tất cả các tỉnh/thành
        /// </summary>
        /// <returns>Danh sách các đối tượng Province</returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            // Lấy danh sách tỉnh thành và sắp xếp theo tên (Alpha b)
            string sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";

            var data = await connection.QueryAsync<Province>(sql);
            return data.ToList();
        }
        /// <summary>
        /// Lấy danh sách tất cả các tỉnh/thành (Hàm đồng bộ)
        /// </summary>
        public List<Province> List()
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";
            return connection.Query<Province>(sql).ToList();
        }
    }
}