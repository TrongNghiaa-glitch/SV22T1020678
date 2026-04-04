using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Sales;
using System.Data;

namespace SV22T1020678.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho đơn hàng và chi tiết đơn hàng
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Xử lý dữ liệu bảng Orders

        public async Task<int> AddAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Cần xóa chi tiết đơn hàng trước khi xóa đơn hàng để tránh lỗi ràng buộc khóa ngoại
            string sql = @"
                DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                DELETE FROM Orders WHERE OrderID = @OrderID;";

            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
            return rowsAffected > 0;
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT 
                    o.*,
                    c.CustomerName, c.ContactName as CustomerContactName, c.Address as CustomerAddress, c.Phone as CustomerPhone, c.Email as CustomerEmail,
                    e.FullName as EmployeeName,
                    s.ShipperName, s.Phone as ShipperPhone
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE o.OrderID = @OrderID";

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            string searchCondition = $"%{input.SearchValue}%";
            int statusFilter = (int)input.Status;

            // Câu lệnh đếm số lượng đơn hàng
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE (@SearchValue = N'%%' OR c.CustomerName LIKE @SearchValue)
                  AND (@Status = 0 OR o.Status = @Status)
                  AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                  AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new
            {
                SearchValue = searchCondition,
                Status = statusFilter,
                DateFrom = input.DateFrom,
                DateTo = input.DateTo
            });

            var result = new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<OrderViewInfo>()
            };

            if (rowCount == 0) return result;

            // Câu lệnh truy vấn dữ liệu
            string sqlData;
            if (input.PageSize == 0)
            {
                sqlData = @"
                    SELECT 
                        o.*,
                        c.CustomerName, c.ContactName as CustomerContactName, c.Address as CustomerAddress, c.Phone as CustomerPhone, c.Email as CustomerEmail,
                        e.FullName as EmployeeName,
                        s.ShipperName, s.Phone as ShipperPhone
                    FROM Orders o
                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                    WHERE (@SearchValue = N'%%' OR c.CustomerName LIKE @SearchValue)
                      AND (@Status = 0 OR o.Status = @Status)
                      AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                      AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
                    ORDER BY o.OrderTime DESC";

                var data = await connection.QueryAsync<OrderViewInfo>(sqlData, new
                {
                    SearchValue = searchCondition,
                    Status = statusFilter,
                    DateFrom = input.DateFrom,
                    DateTo = input.DateTo
                });
                result.DataItems = data.ToList();
            }
            else
            {
                sqlData = @"
                    SELECT 
                        o.*,
                        c.CustomerName, c.ContactName as CustomerContactName, c.Address as CustomerAddress, c.Phone as CustomerPhone, c.Email as CustomerEmail,
                        e.FullName as EmployeeName,
                        s.ShipperName, s.Phone as ShipperPhone
                    FROM Orders o
                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                    WHERE (@SearchValue = N'%%' OR c.CustomerName LIKE @SearchValue)
                      AND (@Status = 0 OR o.Status = @Status)
                      AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                      AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
                    ORDER BY o.OrderTime DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<OrderViewInfo>(sqlData, new
                {
                    SearchValue = searchCondition,
                    Status = statusFilter,
                    DateFrom = input.DateFrom,
                    DateTo = input.DateTo,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data.ToList();
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Orders
                SET CustomerID = @CustomerID,
                    OrderTime = @OrderTime,
                    DeliveryProvince = @DeliveryProvince,
                    DeliveryAddress = @DeliveryAddress,
                    EmployeeID = @EmployeeID,
                    AcceptTime = @AcceptTime,
                    ShipperID = @ShipperID,
                    ShippedTime = @ShippedTime,
                    FinishedTime = @FinishedTime,
                    Status = @Status
                WHERE OrderID = @OrderID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        #endregion

        #region Xử lý trạng thái đơn hàng (Mới thêm)

        public async Task<bool> AcceptAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Status = 2 (Đã duyệt), cập nhật AcceptTime
            string sql = @"UPDATE Orders 
                           SET Status = 2, AcceptTime = GETDATE() 
                           WHERE OrderID = @OrderID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
            return rowsAffected > 0;
        }

        public async Task<bool> ShipAsync(int orderID, int shipperID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Status = 3 (Đang giao), cập nhật ShipperID và ShippedTime
            string sql = @"UPDATE Orders 
                           SET Status = 3, ShipperID = @ShipperID, ShippedTime = GETDATE() 
                           WHERE OrderID = @OrderID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID, ShipperID = shipperID });
            return rowsAffected > 0;
        }

        public async Task<bool> FinishAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Status = 4 (Hoàn tất), cập nhật FinishedTime
            string sql = @"UPDATE Orders 
                           SET Status = 4, FinishedTime = GETDATE() 
                           WHERE OrderID = @OrderID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
            return rowsAffected > 0;
        }

        public async Task<bool> CancelAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Status = -1 (Hủy)
            string sql = @"UPDATE Orders 
                           SET Status = -1 
                           WHERE OrderID = @OrderID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
            return rowsAffected > 0;
        }

        public async Task<bool> RejectAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            // Status = -2 (Từ chối)
            string sql = @"UPDATE Orders 
                           SET Status = -2 
                           WHERE OrderID = @OrderID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID });
            return rowsAffected > 0;
        }

        #endregion

        #region Xử lý dữ liệu bảng OrderDetails

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";

            int rowsAffected = await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID });
            return rowsAffected > 0;
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT od.*, p.ProductName, p.Unit, p.Photo 
                FROM OrderDetails od
                JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID = @OrderID AND od.ProductID = @ProductID";

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
        }

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT od.*, p.ProductName, p.Unit, p.Photo 
                FROM OrderDetails od
                JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID = @OrderID";

            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID });
            return data.ToList();
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE OrderDetails
                SET Quantity = @Quantity,
                    SalePrice = @SalePrice
                WHERE OrderID = @OrderID AND ProductID = @ProductID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lưu đơn hàng và chi tiết đơn hàng (Sử dụng Transaction để đảm bảo toàn vẹn dữ liệu)
        /// </summary>
        public async Task<int> SaveOrderAsync(Order data, IEnumerable<OrderDetail> details)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 1. Thêm thông tin vào bảng Orders và lấy OrderID vừa được tạo ra
                string sqlOrder = @"
                    INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, Status)
                    VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @Status);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int orderID = await connection.ExecuteScalarAsync<int>(sqlOrder, data, transaction);

                // 2. Thêm từng mặt hàng vào bảng OrderDetails
                string sqlDetail = @"
                    INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                    VALUES (@OrderID, @ProductID, @Quantity, @SalePrice);";

                foreach (var item in details)
                {
                    await connection.ExecuteAsync(sqlDetail, new
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    }, transaction);
                }

                // Nếu không có lỗi gì xảy ra thì xác nhận lưu toàn bộ
                await transaction.CommitAsync();
                return orderID;
            }
            catch
            {
                // Nếu có lỗi ở bất kỳ bước nào thì hủy bỏ toàn bộ thao tác (Rollback)
                await transaction.RollbackAsync();
                return 0;
            }
        }

        #endregion
    }
}