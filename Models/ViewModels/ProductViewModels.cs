using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Models.ViewModels
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();

        public int? SelectedCategoryId { get; set; }
        public int? SelectedBrandId { get; set; }
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 12;
    }

    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
        public bool CanReview { get; set; }
        public bool AlreadyInWishlist { get; set; }
        public Dictionary<int, int> RatingBreakdown { get; set; } = new();
    }
}
