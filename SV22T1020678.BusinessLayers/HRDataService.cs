using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.DataLayers.SQLServer;
using SV22T1020678.Models.Common;
using SV22T1020678.Models.HR;

namespace SV22T1020678.BusinessLayers
{
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input) => await employeeDB.ListAsync(input);

        public static async Task<Employee?> GetEmployeeAsync(int id) => await employeeDB.GetAsync(id);

        public static async Task<int> AddEmployeeAsync(Employee data) => await employeeDB.AddAsync(data);

        public static async Task<bool> UpdateEmployeeAsync(Employee data) => await employeeDB.UpdateAsync(data);

        public static async Task<bool> DeleteEmployeeAsync(int id)
        {
            if (await employeeDB.IsUsedAsync(id)) return false;
            return await employeeDB.DeleteAsync(id);
        }

        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int id = 0) => await employeeDB.ValidateEmailAsync(email, id);
    }
}