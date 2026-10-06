using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;

namespace WebBanCameraGiamSat.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 9;
            var query = _context.NewsPosts.Where(n => n.IsPublished).OrderByDescending(n => n.CreatedDate);
            int total = await query.CountAsync();
            var posts = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            return View(posts);
        }

        public async Task<IActionResult> Details(string slug)
        {
            var post = await _context.NewsPosts.FirstOrDefaultAsync(n => n.Slug == slug && n.IsPublished);
            if (post == null) return NotFound();

            post.ViewCount += 1;
            await _context.SaveChangesAsync();

            var others = await _context.NewsPosts.Where(n => n.IsPublished && n.Id != post.Id)
                .OrderByDescending(n => n.CreatedDate).Take(4).ToListAsync();
            ViewBag.OtherPosts = others;

            return View(post);
        }
    }
}
