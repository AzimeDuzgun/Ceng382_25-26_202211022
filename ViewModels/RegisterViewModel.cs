using System.ComponentModel.DataAnnotations;
using DZGNCatering.Models; // Rol yapısını tanımak için ekledik

namespace DZGNCatering.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad Soyad boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "Adınız çok uzun.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "E-posta boş bırakılamaz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre boş bırakılamaz.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı boş bırakılamaz.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Lütfen bir hesap türü seçin.")]
        public UserRole Role { get; set; }

        [Required(ErrorMessage = "Lütfen ilçe/hizmet bölgenizi seçin.")]
        public string District { get; set; }
    }
}
