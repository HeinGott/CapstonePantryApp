namespace Pantreats.Models
{
    public class ItemRequest
    {
        public int Id { get; set; }
        public string? SelectedItems { get; set; }
        public string? RequestedItem { get; set; }
        public string UserName { get; set; }
    }
}
