using System.ComponentModel.DataAnnotations;

namespace DZGNCatering.Models
{
    // Hocanın istediği 3 farklı rolü burada Enum olarak tanımlıyoruz. 
    // Veritabanında karmaşa yaratmamak için en temiz yöntem budur.
    public enum UserRole
    {
        Admin,
        Caretaker,
        User
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Şifreleri açık açık yazmayacağız, güvenlik puanı buradan gelecek.

        [Required]
        public UserRole Role { get; set; } = UserRole.User; // Sisteme yeni kayıt olan herkes varsayılan olarak User (Müşteri) olur.

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Müşteri için kendi oturduğu ilçe, Firma için ise hizmet verdiği ilçe!
        public string District { get; set; } = "Çankaya"; // Varsayılan olarak bir ilçe atayalım boş kalmasın
    }
}