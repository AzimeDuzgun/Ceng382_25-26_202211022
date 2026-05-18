using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DZGNCatering.Models
{
    public enum OrderStatus
    {
        Beklemede,
        Hazırlanıyor,
        Yolda,
        TeslimEdildi,
        IptalEdildi
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int MenuId { get; set; }
        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        [Required(ErrorMessage = "Adet belirtilmelidir.")]
        [Range(1, 100, ErrorMessage = "En az 1, en fazla 100 adet sipariş verebilirsiniz.")]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required(ErrorMessage = "Teslimat adresi zorunludur.")]
        [StringLength(500)]
        public string DeliveryAddress { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Beklemede;

        public DateTime OrderDate { get; set; } = DateTime.Now;
    }
}