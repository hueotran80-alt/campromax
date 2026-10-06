namespace WebBanCameraGiamSat.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockCount { get; set; }

        public List<string> RevenueChartLabels { get; set; } = new();
        public List<decimal> RevenueChartData { get; set; } = new();

        public List<TopProductVM> TopSellingProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();

        public List<OrderStatusCountVM> OrderStatusCounts { get; set; } = new();
    }

    public class TopProductVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int SoldCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrderStatusCountVM
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
