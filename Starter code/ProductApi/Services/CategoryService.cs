using Microsoft.Extensions.Logging;
using ProductApi.Common;
using ProductApi.Mappings;
using ProductApi.Models;
using ProductApi.Models.Dtos;
using ProductApi.Repositories;

namespace ProductApi.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository repository, ILogger<CategoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<CategoryResponse>>> GetAllAsync()
    {
        try
        {
            List<Category> categories = await _repository.GetAllAsync();
            List<CategoryResponse> response = categories.Select(c => c.ToResponse()).ToList();
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe kategorioiden haussa");
            return Result.Failure<List<CategoryResponse>>("Kategorioiden haku epäonnistui");
        }
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(int id)
    {
        try
        {
            Category? category = await _repository.GetByIdAsync(id);

            if (category == null)
                return Result.Failure<CategoryResponse>($"Kategoriaa {id} ei löytynyt");

            return Result.Success(category.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe kategorian haussa: {CategoryId}", id);
            return Result.Failure<CategoryResponse>("Kategorian haku epäonnistui");
        }
    }

    public async Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request)
    {
        try
        {
            Category category = request.ToEntity();
            Category created = await _repository.AddAsync(category);
            return Result.Success(created.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe kategorian luomisessa: {CategoryName}", request.Name);
            return Result.Failure<CategoryResponse>("Kategorian luominen epäonnistui");
        }
    }

    public async Task<Result<CategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        try
        {
            Category? existing = await _repository.GetByIdAsync(id);

            if (existing == null)
                return Result.Failure<CategoryResponse>($"Kategoriaa {id} ei löytynyt");

            request.UpdateEntity(existing);
            await _repository.UpdateAsync(existing);
            return Result.Success(existing.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe kategorian päivittämisessä: {CategoryId}", id);
            return Result.Failure<CategoryResponse>("Kategorian päivittäminen epäonnistui");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            bool deleted = await _repository.DeleteAsync(id);

            if (!deleted)
                return Result.Failure($"Kategoriaa {id} ei löytynyt");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe kategorian poistamisessa: {CategoryId}", id);
            return Result.Failure("Kategorian poistaminen epäonnistui");
        }
    }
}
