using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;

namespace WebBanCameraGiamSat.Components
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        public CategoryMenuViewComponent(ApplicationDbContext context) { _context = context; }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            return View(categories);
        }
    }
}
