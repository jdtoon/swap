using NetMXApp.Dtos;

namespace NetMXApp.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(Guid id);
}