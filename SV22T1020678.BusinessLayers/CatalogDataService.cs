using SV22T1020678.DataLayers.Interfaces;
using SV22T1020678.DataLayers.SQLServer;
using SV22T1020678.Models.Catalog;
using SV22T1020678.Models.Common;

namespace SV22T1020678.BusinessLayers
{
    public static class CatalogDataService
    {
        private static readonly IProductRepository productDB;
        private static readonly IGenericRepository<Category> categoryDB;

        static CatalogDataService()
        {
            productDB = new ProductRepository(Configuration.ConnectionString);
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
        }

        #region Nghiệp vụ Loại hàng (Category)
        public static async Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input) => await categoryDB.ListAsync(input);
        public static async Task<Category?> GetCategoryAsync(int id) => await categoryDB.GetAsync(id);
        public static async Task<int> AddCategoryAsync(Category data) => await categoryDB.AddAsync(data);
        public static async Task<bool> UpdateCategoryAsync(Category data) => await categoryDB.UpdateAsync(data);
        public static async Task<bool> DeleteCategoryAsync(int id)
        {
            if (await categoryDB.IsUsedAsync(id)) return false;
            return await categoryDB.DeleteAsync(id);
        }
        #endregion

        #region Nghiệp vụ Mặt hàng (Product)
        public static async Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input) => await productDB.ListAsync(input);
        public static async Task<Product?> GetProductAsync(int id) => await productDB.GetAsync(id);
        public static async Task<int> AddProductAsync(Product data) => await productDB.AddAsync(data);
        public static async Task<bool> UpdateProductAsync(Product data) => await productDB.UpdateAsync(data);
        public static async Task<bool> DeleteProductAsync(int id)
        {
            // ĐÃ SỬA TẠI ĐÂY: InUsedAsync -> IsUsedAsync
            if (await productDB.IsUsedAsync(id)) return false;
            return await productDB.DeleteAsync(id);
        }
        #endregion

        #region Nghiệp vụ Ảnh Mặt hàng (ProductPhoto)
        public static async Task<List<ProductPhoto>> ListPhotosAsync(int productId) => await productDB.ListPhotosAsync(productId);
        public static async Task<ProductPhoto?> GetPhotoAsync(long photoId) => await productDB.GetPhotoAsync(photoId);
        public static async Task<long> AddPhotoAsync(ProductPhoto data) => await productDB.AddPhotoAsync(data);
        public static async Task<bool> UpdatePhotoAsync(ProductPhoto data) => await productDB.UpdatePhotoAsync(data);
        public static async Task<bool> DeletePhotoAsync(long photoId) => await productDB.DeletePhotoAsync(photoId);
        #endregion

        #region Nghiệp vụ Thuộc tính Mặt hàng (ProductAttribute)
        public static async Task<List<ProductAttribute>> ListAttributesAsync(int productId) => await productDB.ListAttributesAsync(productId);
        public static async Task<ProductAttribute?> GetAttributeAsync(long attributeId) => await productDB.GetAttributeAsync(attributeId);
        public static async Task<long> AddAttributeAsync(ProductAttribute data) => await productDB.AddAttributeAsync(data);
        public static async Task<bool> UpdateAttributeAsync(ProductAttribute data) => await productDB.UpdateAttributeAsync(data);
        public static async Task<bool> DeleteAttributeAsync(long attributeId) => await productDB.DeleteAttributeAsync(attributeId);
        #endregion
    }
}