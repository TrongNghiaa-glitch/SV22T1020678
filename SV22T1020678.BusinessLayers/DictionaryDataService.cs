using System.Collections.Generic;
using System.Threading.Tasks;
using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.DataLayers.SQLServer;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.DataDictionary;
using SV22T1020678.Models.HR;
using SV22T1020678.Models.Partner;

namespace SV22T1020678.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng tác nghiệp/nghiệp vụ cho các dữ liệu từ điển (danh mục) và các đối tượng khác
    /// </summary>
    public static class DictionaryDataService
    {
        // Khai báo các biến Repository (Dùng readonly để đảm bảo chỉ được khởi tạo 1 lần)
        private static readonly IDataDictionaryRepository<Province> provinceDB;
        private static readonly IGenericRepository<Category> categoryDB;
        private static readonly IGenericRepository<Supplier> supplierDB;
        private static readonly IGenericRepository<Shipper> shipperDB;
        private static readonly ICustomerRepository customerDB;
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// BẮT BUỘC PHẢI CÓ HÀM TẠO TĨNH NÀY (Static Constructor).
        /// Nó tự động chạy và khởi tạo tất cả các biến DB ở trên bằng chuỗi kết nối từ Configuration.
        /// Giúp khắc phục triệt để lỗi NullReferenceException.
        /// </summary>
        static DictionaryDataService()
        {
            string connectionString = Configuration.ConnectionString;

            provinceDB = new ProvinceRepository(connectionString);
            categoryDB = new CategoryRepository(connectionString);
            supplierDB = new SupplierRepository(connectionString);
            shipperDB = new ShipperRepository(connectionString);
            customerDB = new CustomerRepository(connectionString);
            employeeDB = new EmployeeRepository(connectionString);
        }

        #region --- Xử lý cho Tỉnh/Thành (Province) ---
        // Đã đồng bộ tên hàm là ListProvincesAsync để khớp với SelectListHelper của bạn
        public static async Task<List<Province>> ListProvincesAsync() => await provinceDB.ListAsync();

        // Giữ lại hàm cũ để phòng trường hợp các controller khác của bạn đang dùng tên này
        public static async Task<List<Province>> ListOfProvinces() => await provinceDB.ListAsync();
        #endregion

        #region --- Xử lý cho Loại hàng (Category) ---
        public static async Task<PagedResult<Category>> ListOfCategories(PaginationSearchInput input) => await categoryDB.ListAsync(input);
        public static async Task<Category?> GetCategory(int id) => await categoryDB.GetAsync(id);
        public static async Task<int> AddCategory(Category data) => await categoryDB.AddAsync(data);
        public static async Task<bool> UpdateCategory(Category data) => await categoryDB.UpdateAsync(data);
        public static async Task<bool> DeleteCategory(int id) => await categoryDB.DeleteAsync(id);
        public static async Task<bool> InUsedCategory(int id) => await categoryDB.IsUsedAsync(id);
        #endregion

        #region --- Xử lý cho Nhà cung cấp (Supplier) ---
        public static async Task<PagedResult<Supplier>> ListOfSuppliers(PaginationSearchInput input) => await supplierDB.ListAsync(input);
        public static async Task<Supplier?> GetSupplier(int id) => await supplierDB.GetAsync(id);
        public static async Task<int> AddSupplier(Supplier data) => await supplierDB.AddAsync(data);
        public static async Task<bool> UpdateSupplier(Supplier data) => await supplierDB.UpdateAsync(data);
        public static async Task<bool> DeleteSupplier(int id) => await supplierDB.DeleteAsync(id);
        public static async Task<bool> InUsedSupplier(int id) => await supplierDB.IsUsedAsync(id);
        #endregion

        #region --- Xử lý cho Người giao hàng (Shipper) ---
        public static async Task<PagedResult<Shipper>> ListOfShippers(PaginationSearchInput input) => await shipperDB.ListAsync(input);
        public static async Task<Shipper?> GetShipper(int id) => await shipperDB.GetAsync(id);
        public static async Task<int> AddShipper(Shipper data) => await shipperDB.AddAsync(data);
        public static async Task<bool> UpdateShipper(Shipper data) => await shipperDB.UpdateAsync(data);
        public static async Task<bool> DeleteShipper(int id) => await shipperDB.DeleteAsync(id);
        public static async Task<bool> InUsedShipper(int id) => await shipperDB.IsUsedAsync(id);
        #endregion

        #region --- Xử lý cho Khách hàng (Customer) ---
        public static async Task<PagedResult<Customer>> ListOfCustomers(PaginationSearchInput input) => await customerDB.ListAsync(input);
        public static async Task<Customer?> GetCustomer(int id) => await customerDB.GetAsync(id);
        public static async Task<int> AddCustomer(Customer data) => await customerDB.AddAsync(data);
        public static async Task<bool> UpdateCustomer(Customer data) => await customerDB.UpdateAsync(data);
        public static async Task<bool> DeleteCustomer(int id) => await customerDB.DeleteAsync(id);
        public static async Task<bool> InUsedCustomer(int id) => await customerDB.IsUsedAsync(id);
        public static async Task<bool> IsValidCustomerEmail(string email, int id = 0) => await customerDB.IsValidEmailAsync(email, id);
        #endregion

        #region --- Xử lý cho Nhân viên (Employee) ---
        public static async Task<PagedResult<Employee>> ListOfEmployees(PaginationSearchInput input) => await employeeDB.ListAsync(input);
        public static async Task<Employee?> GetEmployee(int id) => await employeeDB.GetAsync(id);
        public static async Task<int> AddEmployee(Employee data) => await employeeDB.AddAsync(data);
        public static async Task<bool> UpdateEmployee(Employee data) => await employeeDB.UpdateAsync(data);
        public static async Task<bool> DeleteEmployee(int id) => await employeeDB.DeleteAsync(id);
        public static async Task<bool> InUsedEmployee(int id) => await employeeDB.IsUsedAsync(id);
        public static async Task<bool> ValidateEmployeeEmail(string email, int id = 0) => await employeeDB.ValidateEmailAsync(email, id);
        #endregion
    }
}