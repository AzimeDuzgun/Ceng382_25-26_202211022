using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DZGNCatering.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [Required(ErrorMessage = "Lütfen yemek menüsü için puan seçin.")]
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int MenuRating { get; set; } // Menü Puanı (1-5)

        [Required(ErrorMessage = "Lütfen yemek firması için puan seçin.")]
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int CaretakerRating { get; set; } // Firma Hizmet Puanı (1-5)

        [Required(ErrorMessage = "Yorum alanı boş bırakılamaz.")]
        [StringLength(500, ErrorMessage = "Yorumunuz en fazla 500 karakter olabilir.")]
        public string Comment { get; set; } // Müşteri Yorumu

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}