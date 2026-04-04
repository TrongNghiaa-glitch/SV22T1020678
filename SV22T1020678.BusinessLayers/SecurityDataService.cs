using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.DataLayers.SQLServer;
using SV22T1020678.Models.Security;

namespace SV22T1020678.BusinessLayers
{
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository employeeAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        static SecurityDataService()
        {
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
        }

        #region Tài khoản Nhân viên (Dùng cho trang Admin)
        public static async Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password)
            => await employeeAccountDB.Authorize(userName, password);

        public static async Task<bool> ChangeEmployeePasswordAsync(string userName, string password)
            => await employeeAccountDB.ChangePassword(userName, password);
        #endregion

        #region Tài khoản Khách hàng (Dùng cho trang ShopFront-end)
        public static async Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password)
            => await customerAccountDB.Authorize(userName, password);

        public static async Task<bool> ChangeCustomerPasswordAsync(string userName, string password)
            => await customerAccountDB.ChangePassword(userName, password);
        #endregion
    }
}