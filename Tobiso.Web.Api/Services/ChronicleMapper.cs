using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

/// <summary>
/// Shared entity-to-DTO projection for kronika items, reused by <see cref="ChronicleItemService"/>
/// and <see cref="ChronicleAxisService"/>. Assumes Categories.Category, Regions.Region, Sources and
/// MinGrade navigations are already loaded (via Include) on <paramref name="item"/>.
/// </summary>
internal static class ChronicleMapper
{
    public static ChronicleItemResponse ToResponse(ChronicleItem item)
    {
        var primary = item.Categories.FirstOrDefault(c => c.IsPrimary) ?? item.Categories.FirstOrDefault();

        return new ChronicleItemResponse
        {
            Id = item.Id,
            Slug = item.Slug,
            ItemType = item is ChroniclePerson ? ChronicleItemTypeConstants.Person : ChronicleItemTypeConstants.Event,
            Title = item.Title,
            Summary = item.Summary,
            Content = item.Content,
            StartYear = item.StartYear,
            EndYear = item.EndYear,
            Precision = item.Precision,
            Reliability = item.Reliability,
            ReliabilityNote = item.ReliabilityNote,
            Importance = item.Importance,
            MinGradeId = item.MinGradeId,
            MinGradeLevel = item.MinGrade?.Level,
            LessonSlug = item.LessonSlug,
            Occupation = (item as ChroniclePerson)?.Occupation,
            Categories = item.Categories
                .Where(c => c.Category != null)
                .Select(c => new ChronicleItemCategoryRef
                {
                    CategoryId = c.CategoryId,
                    Name = c.Category!.Name,
                    Color = c.Category.Color,
                    IsPrimary = c.IsPrimary
                })
                .ToList(),
            Regions = item.Regions
                .Where(r => r.Region != null)
                .Select(r => new ChronicleRegionRef { RegionId = r.RegionId, Name = r.Region!.Name })
                .ToList(),
            Sources = item.Sources
                .Select(s => new ChronicleSourceResponse { Id = s.Id, ItemId = s.ItemId, Title = s.Title, Url = s.Url, Note = s.Note })
                .ToList(),
            PrimaryColor = primary?.Category?.Color,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
