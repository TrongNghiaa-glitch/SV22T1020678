namespace SV22T1020678.BusinessLayers
{
    /// <summary>
    /// Lớp lưu giữ các thông tin cấu hình cần sử dụng cho BusinessLayer
    /// </summary>
    // ĐỔI 'internal' THÀNH 'public' ĐỂ DỰ ÁN ADMIN CÓ THỂ TRUY CẬP
    public class Configuration
    {
        private static string _connectionString = "";

        /// <summary>
        /// Khởi tạo cấu hình cho BusinessLayer
        /// </summary>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thuộc tính trả về chuỗi tham số kết nối đến cơ sở dữ liệu
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}