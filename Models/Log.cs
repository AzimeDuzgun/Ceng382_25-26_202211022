using System.ComponentModel.DataAnnotations;

namespace DZGNCatering.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        public string UserEmail { get; set; } // Soru işaretini kaldırdık

        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}