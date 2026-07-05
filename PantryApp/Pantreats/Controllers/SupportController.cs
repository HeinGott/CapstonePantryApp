using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pantreats.Controllers
{
    public class SupportController : Controller
    {

        private readonly ApplicationDbContext _context;

        public SupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Search(string keyword) //Assistance from Claude used
        {
            if(string.IsNullOrWhiteSpace(keyword))
            {
                return View(new List<SupportArticle>());
            }

            var allArticles = _context.SupportArticles.ToList();

            var results = allArticles
                .Where(a => a.Title.Contains(keyword) || a.Keywords.Any(k => k.Contains(keyword)))
                .ToList();

            if(results.Count == 1)
            {
                return RedirectToAction("Article", new { slug = results[0].Slug });
            }
            return View(results);
        }

        [HttpGet]
        public IActionResult Article(string slug)
        {
            var article = _context.SupportArticles.FirstOrDefault(a => a.Slug == slug);
            if (article == null) return NotFound();

            return View(article);
        }
        

    }
}
