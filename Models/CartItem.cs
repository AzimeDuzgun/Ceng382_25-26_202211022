namespace DZGNCatering.Models
{
    public class CartItem
    {
        public int MenuId { get; set; }
        public string MenuName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int CaretakerId { get; set; } // Hangi firmanın yemeği? (Sipariş ayırırken lazım olacak)
        public string CaretakerName { get; set; }

        // Ara Toplam (Adet x Fiyat)
        public decimal SubTotal => Price * Quantity;
    }
}