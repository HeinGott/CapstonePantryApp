using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Areas.Identity.Pages.Account
{
    [Authorize(Roles = "Admin")]
    public class ItemRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ItemRequestsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ItemRequest> Requests { get; set; } = new();

        public async Task OnGetAsync()
        {
            Requests = await _context.ItemRequest.ToListAsync();
        }
    }
}