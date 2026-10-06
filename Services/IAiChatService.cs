namespace WebBanCameraGiamSat.Services
{
    public interface IAiChatService
    {
        Task<AiChatResponse> ProcessMessageAsync(string userId, string message);
    }

    public class AiChatResponse
    {
        public string Reply { get; set; } = string.Empty;
        public bool OrderPlaced { get; set; } = false;
        public string? OrderCode { get; set; }
        public decimal TotalAmount { get; set; }
        public bool AddedToCart { get; set; } = false;
        public int CartCount { get; set; }
        public List<AiProductSuggestion>? Suggestions { get; set; }
    }

    public class AiProductSuggestion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}
