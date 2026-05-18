using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DZGNCatering.Models
{
    public class Menu
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Menü adı boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "Menü adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Menü içeriği/açıklaması boş bırakılamaz.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Fiyat boş bırakılamaz.")]
        [Range(0.01, 10000.00, ErrorMessage = "Geçerli bir fiyat giriniz.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } // Soru işaretini kaldırdık

        [StringLength(200, ErrorMessage = "Özelleştirme tanımı çok uzun.")]
        public string CustomizationDefinitions { get; set; } // Soru işaretini kaldırdık

        [Required]
        public int CaretakerId { get; set; }

        [ForeignKey("CaretakerId")]
        public virtual User Caretaker { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Menü müşterilere görünüyor mu? (Soft Delete kontrolü)
        public bool IsActive { get; set; } = true;
    }
}