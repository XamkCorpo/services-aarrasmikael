using Microsoft.Extensions.Logging;
using ProductApi.Common;
using ProductApi.Mappings;
using ProductApi.Models;
using ProductApi.Models.Dtos;
using ProductApi.Repositories;

namespace ProductApi.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<ProductResponse>>> GetAllAsync()
    {
        try
        {
            List<Product> products = await _repository.GetAllAsync();
            List<ProductResponse> response = products.Select(p => p.ToResponse()).ToList();
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe tuotteiden haussa");
            return Result.Failure<List<ProductResponse>>("Tuotteiden haku epäonnistui");
        }
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(int id)
    {
        try
        {
            Product? product = await _repository.GetByIdAsync(id);

            if (product == null)
                return Result.Failure<ProductResponse>($"Tuotetta {id} ei löytynyt");

            return Result.Success(product.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe tuotteen haussa: {ProductId}", id);
            return Result.Failure<ProductResponse>("Tuotteen haku epäonnistui");
        }
    }

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request)
    {
        try
        {
            Product product = request.ToEntity();
            Product created = await _repository.AddAsync(product);
            return Result.Success(created.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe tuotteen luomisessa: {ProductName}", request.Name);
            return Result.Failure<ProductResponse>("Tuotteen luominen epäonnistui");
        }
    }

    public async Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request)
    {
        try
        {
            Product? existing = await _repository.GetByIdAsync(id);

            if (existing == null)
                return Result.Failure<ProductResponse>($"Tuotetta {id} ei löytynyt");

            request.UpdateEntity(existing);
            await _repository.UpdateAsync(existing);
            return Result.Success(existing.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe tuotteen päivittämisessä: {ProductId}", id);
            return Result.Failure<ProductResponse>("Tuotteen päivittäminen epäonnistui");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            bool deleted = await _repository.DeleteAsync(id);

            if (!deleted)
                return Result.Failure($"Tuotetta {id} ei löytynyt");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe tuotteen poistamisessa: {ProductId}", id);
            return Result.Failure("Tuotteen poistaminen epäonnistui");
        }
    }
}
