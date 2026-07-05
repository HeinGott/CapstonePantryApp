namespace Pantreats.Models
{
    public class SupportArticle //Claude used to get started
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; } //mapping to route
        public string[]Keywords { get; set; } //search terms
        public string Summary { get; set; }//shown amongst search results
        public string Content { get; set; }//full-text matching
    }
}
