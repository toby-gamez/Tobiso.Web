using System.ComponentModel.DataAnnotations;

namespace Tobiso.Web.Shared.DTOs;

public class RelatedPostResponse
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int RelatedPostId { get; set; }
    public string? Text { get; set; }
    
    // Navigační properties pro UI
    public string? PostTitle { get; set; }
    public string? RelatedPostTitle { get; set; }
}

public class CreateRelatedPostRequest
{
    [Required(ErrorMessage = "Hlavní post je povinný")]
    [Range(1, int.MaxValue, ErrorMessage = "Musíte vybrat platný post")]
    public int PostId { get; set; }
    
    [Required(ErrorMessage = "Souvisejíci post je povinný")]
    [Range(1, int.MaxValue, ErrorMessage = "Musíte vybrat platný související post")]
    public int RelatedPostId { get; set; }
    
    [MaxLength(500, ErrorMessage = "Text nesmí být delší než 500 znaků")]
    public string? Text { get; set; }
}

public class UpdateRelatedPostRequest
{
    [Required(ErrorMessage = "Hlavní post je povinný")]
    [Range(1, int.MaxValue, ErrorMessage = "Musíte vybrat platný post")]
    public int PostId { get; set; }
    
    [Required(ErrorMessage = "Souvisejíci post je povinný")]
    [Range(1, int.MaxValue, ErrorMessage = "Musíte vybrat platný související post")]
    public int RelatedPostId { get; set; }
    
    [MaxLength(500, ErrorMessage = "Text nesmí být delší než 500 znaků")]
    public string? Text { get; set; }
}