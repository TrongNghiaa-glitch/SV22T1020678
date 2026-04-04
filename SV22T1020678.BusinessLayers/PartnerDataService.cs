using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.DataLayers.SQLServer;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020678.BusinessLayers
{
    /// <summary>
    /// Lớp cung cấp các chức năng nghiệp vụ liên quan đến các đối tác của hệ thống.
    /// Bao gồm:
    /// - Nhà cung cấp (Supplier)
    /// - Người giao hàng (Shipper)
    /// - Khách hàng (Customer)
    /// 
    /// Các chức năng chính:
    /// - Lấy danh sách có phân trang
    /// - Thêm mới
    /// - Cập nhật
    /// - Xóa
    /// - Kiểm tra dữ liệu đang được sử dụng
    /// </summary>
    public class PartnerDataService
    {
        private static readonly IGenericRepository<Supplier> supplierDB;
        private static readonly IGenericRepository<Shipper> shipperDB;
        private static readonly ICustomerRepository customerDB;

        /// <summary>
        /// Constructor tĩnh khởi tạo các repository
        /// </summary>
        static PartnerDataService()
        {
            supplierDB = new SupplierRepository(Configuration.ConnectionString);
            shipperDB = new ShipperRepository(Configuration.ConnectionString);
            customerDB = new CustomerRepository(Configuration.ConnectionString);
        }

        #region SUPPLIER

        /// <summary>
        /// Lấy danh sách nhà cung cấp có phân trang
        /// </summary>
        public static async Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
        {
            return await supplierDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết nhà cung cấp
        /// </summary>
        public static async Task<Supplier?> GetSupplierAsync(int supplierID)
        {
            return await supplierDB.GetAsync(supplierID);
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới
        /// </summary>
        public static async Task<int> AddSupplierAsync(Supplier supplier)
        {
            return await supplierDB.AddAsync(supplier);
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        public static async Task<bool> UpdateSupplierAsync(Supplier supplier)
        {
            return await supplierDB.UpdateAsync(supplier);
        }

        /// <summary>
        /// Xóa nhà cung cấp
        /// </summary>
        public static async Task<bool> DeleteSupplierAsync(int supplierID)
        {
            if (await supplierDB.IsUsedAsync(supplierID))
                return false;

            return await supplierDB.DeleteAsync(supplierID);
        }

        /// <summary>
        /// Kiểm tra nhà cung cấp có đang được sử dụng hay không
        /// </summary>
        public static async Task<bool> IsUsedSupplierAsync(int supplierID)
        {
            return await supplierDB.IsUsedAsync(supplierID);
        }

        #endregion


        #region SHIPPER

        /// <summary>
        /// Lấy danh sách người giao hàng có phân trang
        /// </summary>
        public static async Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
        {
            return await shipperDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết người giao hàng
        /// </summary>
        public static async Task<Shipper?> GetShipperAsync(int shipperID)
        {
            return await shipperDB.GetAsync(shipperID);
        }

        /// <summary>
        /// Thêm mới người giao hàng
        /// </summary>
        public static async Task<int> AddShipperAsync(Shipper shipper)
        {
            return await shipperDB.AddAsync(shipper);
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        public static async Task<bool> UpdateShipperAsync(Shipper shipper)
        {
            return await shipperDB.UpdateAsync(shipper);
        }

        /// <summary>
        /// Xóa người giao hàng
        /// </summary>
        public static async Task<bool> DeleteShipperAsync(int shipperID)
        {
            if (await shipperDB.IsUsedAsync(shipperID))
                return false;

            return await shipperDB.DeleteAsync(shipperID);
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng hay không
        /// </summary>
        public static async Task<bool> IsUsedShipperAsync(int shipperID)
        {
            return await shipperDB.IsUsedAsync(shipperID);
        }

        #endregion


        #region CUSTOMER

        /// <summary>
        /// Lấy danh sách khách hàng có phân trang
        /// </summary>
        public static async Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input)
        {
            return await customerDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết khách hàng
        /// </summary>
        public static async Task<Customer?> GetCustomerAsync(int customerID)
        {
            return await customerDB.GetAsync(customerID);
        }

        /// <summary>
        /// Thêm mới khách hàng
        /// </summary>
        public static async Task<int> AddCustomerAsync(Customer customer)
        {
            return await customerDB.AddAsync(customer);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        public static async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            return await customerDB.UpdateAsync(customer);
        }

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        public static async Task<bool> DeleteCustomerAsync(int customerID)
        {
            if (await customerDB.IsUsedAsync(customerID))
                return false;

            return await customerDB.DeleteAsync(customerID);
        }

        /// <summary>
        /// Kiểm tra khách hàng có đang được sử dụng hay không
        /// </summary>
        public static async Task<bool> IsUsedCustomerAsync(int customerID)
        {
            return await customerDB.IsUsedAsync(customerID);
        }

        /// <summary>
        /// Kiểm tra email của khách hàng có hợp lệ hay không.
        /// Email hợp lệ nếu không trùng với email của khách hàng khác.
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="customerID">
        /// Nếu bằng 0 tức là kiểm tra email của khách hàng mới.
        /// Ngược lại là kiểm tra email của khách hàng đang cập nhật.
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ, False nếu email đã tồn tại
        /// </returns>
        public static async Task<bool> IsValidCustomerEmailAsync(string email, int customerID)
        {
            return await customerDB.IsValidEmailAsync(email, customerID);
        }

        #endregion
    }
}