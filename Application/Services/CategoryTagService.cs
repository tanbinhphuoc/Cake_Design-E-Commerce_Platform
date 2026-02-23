using Application.DTOs;
using Application.Interfaces;

namespace Application.Services
{
    public class CategoryTagService : ICategoryTagService
    {
        private readonly IUnitOfWork _uow;
        public CategoryTagService(IUnitOfWork uow) { _uow = uow; }

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            var categories = await _uow.Categories.FindAsync(c => c.IsActive);
            return categories.OrderBy(c => c.SortOrder).Select(c => new CategoryDto
            { Id = c.Id, Name = c.Name, Description = c.Description, ImageUrl = c.ImageUrl, SortOrder = c.SortOrder, IsActive = c.IsActive }).ToList();
        }

        public async Task<Guid> CreateCategoryAsync(CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Category name is required.");
            if (await _uow.Categories.GetByNameAsync(dto.Name) != null) throw new InvalidOperationException("Category already exists.");
            var cat = new Domain.Entities.Category
            { Id = Guid.NewGuid(), Name = dto.Name, Description = dto.Description ?? string.Empty, ImageUrl = dto.ImageUrl ?? string.Empty, SortOrder = dto.SortOrder, IsActive = true, CreatedAt = DateTime.UtcNow };
            await _uow.Categories.AddAsync(cat);
            await _uow.SaveChangesAsync();
            return cat.Id;
        }

        public async Task<string> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
        {
            var cat = await _uow.Categories.GetByIdAsync(id);
            if (cat == null) throw new ArgumentException("Category not found.");
            if (dto.Name != null) cat.Name = dto.Name;
            if (dto.Description != null) cat.Description = dto.Description;
            if (dto.ImageUrl != null) cat.ImageUrl = dto.ImageUrl;
            if (dto.SortOrder.HasValue) cat.SortOrder = dto.SortOrder.Value;
            if (dto.IsActive.HasValue) cat.IsActive = dto.IsActive.Value;
            await _uow.SaveChangesAsync();
            return "Category updated.";
        }

        public async Task<string> DeleteCategoryAsync(Guid id)
        {
            var cat = await _uow.Categories.GetByIdAsync(id);
            if (cat == null) throw new ArgumentException("Category not found.");
            _uow.Categories.Remove(cat);
            await _uow.SaveChangesAsync();
            return "Category deleted.";
        }

        public async Task<List<TagDto>> GetTagsAsync()
        {
            var tags = await _uow.Tags.GetAllAsync();
            return tags.OrderBy(t => t.Name).Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList();
        }

        public async Task<Guid> CreateTagAsync(CreateTagDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Tag name is required.");
            if (await _uow.Tags.GetByNameAsync(dto.Name) != null) throw new InvalidOperationException("Tag already exists.");
            var tag = new Domain.Entities.Tag { Id = Guid.NewGuid(), Name = dto.Name, CreatedAt = DateTime.UtcNow };
            await _uow.Tags.AddAsync(tag);
            await _uow.SaveChangesAsync();
            return tag.Id;
        }

        public async Task<string> UpdateTagAsync(Guid id, CreateTagDto dto)
        {
            var tag = await _uow.Tags.GetByIdAsync(id);
            if (tag == null) throw new ArgumentException("Tag not found.");
            if (!string.IsNullOrWhiteSpace(dto.Name)) tag.Name = dto.Name;
            await _uow.SaveChangesAsync();
            return "Tag updated.";
        }

        public async Task<string> DeleteTagAsync(Guid id)
        {
            var tag = await _uow.Tags.GetByIdAsync(id);
            if (tag == null) throw new ArgumentException("Tag not found.");
            _uow.Tags.Remove(tag);
            await _uow.SaveChangesAsync();
            return "Tag deleted.";
        }
    }
}
