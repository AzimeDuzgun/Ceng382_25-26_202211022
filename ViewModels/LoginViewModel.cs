using System.ComponentModel.DataAnnotations;

namespace DZGNCatering.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta boş bırakılamaz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre boş bırakılamaz.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
