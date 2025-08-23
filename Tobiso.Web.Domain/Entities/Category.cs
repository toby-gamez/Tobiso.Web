using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tobiso.Web.Domain.Entities;
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Slug { get; set; } = string.Empty;

    public int? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public Category? Parent { get; set; }

    public List<Category> Children { get; set; } = [];

    [NotMapped]
    public string FullPath => Parent == null ? Name : $"{Parent.FullPath} > {Name}";
}
