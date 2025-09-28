using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tobiso.Web.Domain.Entities;

public class RelatedPost
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int PostId { get; set; }
    
    [ForeignKey(nameof(PostId))]
    public Post? Post { get; set; }
    
    [Required]
    public int RelatedPostId { get; set; }
    
    [ForeignKey(nameof(RelatedPostId))]
    public Post? RelatedPostRef { get; set; }
    
    public string? Text { get; set; }
}