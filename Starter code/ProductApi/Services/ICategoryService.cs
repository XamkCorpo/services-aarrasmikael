using ProductApi.Common;
using ProductApi.Models.Dtos;

namespace ProductApi.Services;

public interface ICategoryService
{
    Task<Result<List<CategoryResponse>>> GetAllAsync();
    Task<Result<CategoryResponse>> GetByIdAsync(int id);
    Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request);
    Task<Result<CategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest request);
    Task<Result> DeleteAsync(int id);
}
