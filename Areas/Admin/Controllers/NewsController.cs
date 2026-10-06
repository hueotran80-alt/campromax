using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Helpers;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public NewsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.NewsPosts.OrderByDescending(n => n.CreatedDate).ToListAsync();
            return View(posts);
        }

        public IActionResult Create() => View(new NewsPost { IsPublished = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsPost model)
        {
            ModelState.Remove(nameof(NewsPost.Slug));
            if (!ModelState.IsValid) return View(model);

            model.Slug = SlugHelper.GenerateSlug(model.Title);
            if (await _context.NewsPosts.AnyAsync(n => n.Slug == model.Slug))
                model.Slug += "-" + DateTime.Now.Ticks.ToString().Substring(10);

            model.CreatedDate = DateTime.Now;
            _context.NewsPosts.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm bài viết thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.NewsPosts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsPost model)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove(nameof(NewsPost.Slug));
            if (!ModelState.IsValid) return View(model);

            var post = await _context.NewsPosts.FindAsync(id);
            if (post == null) return NotFound();

            post.Title = model.Title;
            post.Summary = model.Summary;
            post.Content = model.Content;
            post.ImageUrl = model.ImageUrl;
            post.IsPublished = model.IsPublished;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật bài viết thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.NewsPosts.FindAsync(id);
            if (post != null)
            {
                _context.NewsPosts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
