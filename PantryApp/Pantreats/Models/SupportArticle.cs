namespace Pantreats.Models
{
    public class SupportArticle
    {
        public int Id {  get; set; }
        public string Title { get; set; }
        public string Slug  { get; set; }
        public string[]Keywords { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
    }
}
