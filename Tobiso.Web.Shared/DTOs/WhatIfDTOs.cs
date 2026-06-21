namespace Tobiso.Web.Shared.DTOs
{
    public class WhatIfRequest
    {
        public int PostId { get; set; }
        public string UserQuestion { get; set; } = string.Empty;
    }

    public class WhatIfResponse
    {
        public string Scenario { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}
