namespace Tobiso.Web.Shared.DTOs
{
    public class SuggestRelatedResponse
    {
        public List<int> PostIds { get; set; } = new();
        /// <summary>Same posts as PostIds, with an AI-written description and a suggested direction.</summary>
        public List<SuggestedRelation> Items { get; set; } = new();
    }

    public class SuggestRelatedDraftRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        /// <summary>Id of the post being edited (excluded from suggestions); null when creating.</summary>
        public int? PostId { get; set; }
    }

    public class SuggestedRelation
    {
        public int PostId { get; set; }
        public string Text { get; set; } = string.Empty;
        /// <summary>True when the link makes sense from both articles; false when only source -> target.</summary>
        public bool Bidirectional { get; set; } = true;
    }

    /// <summary>A relation chosen in the post form, before it is persisted.</summary>
    public class RelatedLinkSelection
    {
        public int PostId { get; set; }
        public string? Text { get; set; }
        public bool Bidirectional { get; set; } = true;
    }
}
