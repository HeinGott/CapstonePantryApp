/*
 * Represents a donation item submitted from the donation form.
 * Stores the item name, whether the donor selected it,
 * and the quantity being donated.
 */

namespace Pantreats.Models
{
    public class DonationItemInput
    {
        public string Name { get; set; } = "";
        public bool Selected { get; set; }
        public int Quantity { get; set; }
    }
}
