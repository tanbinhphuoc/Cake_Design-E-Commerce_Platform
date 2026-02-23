using Application.DTOs;

namespace Application.Services
{
    public interface ICategoryTagService
    {
        // Categories
        Task<List<CategoryDto>> GetCategoriesAsync();
        Task<Guid> CreateCategoryAsync(CreateCategoryDto dto);
        Task<string> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
        Task<string> DeleteCategoryAsync(Guid id);

        // Tags
        Task<List<TagDto>> GetTagsAsync();
        Task<Guid> CreateTagAsync(CreateTagDto dto);
        Task<string> UpdateTagAsync(Guid id, CreateTagDto dto);
        Task<string> DeleteTagAsync(Guid id);
    }
}
