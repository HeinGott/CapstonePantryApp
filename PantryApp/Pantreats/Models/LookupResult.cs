
namespace Pantreats.Models
{
    public class LookupResult
    {
        //properties need to match json from api response
        public string upc { get; set; }
        public string title { get; set; }
        public string brand { get; set; }
        public string category { get; set; }
        public string description { get; set; }
        public string weight { get; set; }
        public string? images { get; set; }
 

    }
}
