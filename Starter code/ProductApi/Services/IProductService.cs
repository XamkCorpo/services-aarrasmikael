using ProductApi.Common;
using ProductApi.Models.Dtos;

namespace ProductApi.Services;

public interface IProductService
{
    Task<Result<List<ProductResponse>>> GetAllAsync();
    Task<Result<ProductResponse>> GetByIdAsync(int id);
    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request);
    Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request);
    Task<Result> DeleteAsync(int id);
}
