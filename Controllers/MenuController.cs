using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using System;
using System.Linq;
using DZGNCatering.Data;
using DZGNCatering.Models;

namespace DZGNCatering.Controllers
{
    [Authorize(Roles = "Caretaker")]
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            int caretakerId = int.Parse(userIdString);

            var myMenus = _context.Menus
                .Where(m => m.CaretakerId == caretakerId)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            return View(myMenus);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Menu menu, IFormFile imageFile)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            menu.CaretakerId = int.Parse(userIdString);
            ModelState.Remove("Caretaker");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        imageFile.CopyTo(fileStream);
                    }

                    menu.ImageUrl = "/uploads/" + uniqueFileName;
                }
                else
                {
                    menu.ImageUrl = "/css/default-food.jpg";
                }

                _context.Menus.Add(menu);

                // GÜVENLİ LOG MOTORU
                _context.Logs.Add(new DZGNCatering.Models.Log { Message = $"Yeni yemek menüsü yayınladı: {menu.Name}", UserEmail = User.Identity?.Name });

                _context.SaveChanges();

                TempData["Success"] = "Yeni menü başarıyla sisteme eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(menu);
        }

        // ==================== MENÜ DÜZENLEME (GET) ====================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            int currentCaretakerId = int.Parse(userIdString);

            var menu = _context.Menus.FirstOrDefault(m => m.Id == id);
            if (menu == null) return NotFound();

            // Mülkiyet Kontrolü: Başka firmanın menüsüne giremez!
            if (menu.CaretakerId != currentCaretakerId) return Forbid();

            return View(menu);
        }

        // ==================== MENÜ DÜZENLEME (POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Menu model, IFormFile imageFile)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            int currentCaretakerId = int.Parse(userIdString);

            var existingMenu = _context.Menus.FirstOrDefault(m => m.Id == model.Id);
            if (existingMenu == null) return NotFound();

            // Güvenlik: Kendi menüsü değilse patlat
            if (existingMenu.CaretakerId != currentCaretakerId) return Forbid();

            ModelState.Remove("Caretaker"); // Formdan gelmeyen navigation property hata vermesin diye eziyoruz.

            if (ModelState.IsValid)
            {
                // Yeni resim yüklendiyse senin 'uploads' klasörüne kaydediyoruz
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        imageFile.CopyTo(fileStream);
                    }
                    existingMenu.ImageUrl = "/uploads/" + uniqueFileName;
                }

                // Sadece izin verilen alanları güncelliyoruz (Id ve CaretakerId sabit kalır)
                existingMenu.Name = model.Name;
                existingMenu.Description = model.Description;
                existingMenu.Price = model.Price;

                // SOFT DELETE KURTARICI: Gizlenmiş bir menü düzenlenirse otomatik yayına alınır
                existingMenu.IsActive = true;

                _context.Logs.Add(new DZGNCatering.Models.Log { Message = $"Menü güncellendi ve yayına alındı: {existingMenu.Name}", UserEmail = User.Identity?.Name });

                _context.SaveChanges();

                TempData["Success"] = "Menü başarıyla güncellendi ve aktif olarak yayına alındı.";
                return RedirectToAction(nameof(Index)); // Düzenleme bitince Index'e fırlatır!
            }

            return View(model);
        }

        // ==================== MENÜ SİLME (POST) ====================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            int currentCaretakerId = int.Parse(userIdString);

            var menu = _context.Menus.FirstOrDefault(m => m.Id == id);
            if (menu == null) return NotFound();

            if (menu.CaretakerId != currentCaretakerId) return Forbid();

            // Sipariş kontrolü: Sistem bütünlüğü için sipariş alan menü silinemez, SOFT DELETE atılır
            var hasOrders = _context.Orders.Any(o => o.MenuId == id);
            if (hasOrders)
            {
                menu.IsActive = false; // Müşteriden gizle
                _context.Logs.Add(new DZGNCatering.Models.Log { Message = $"Menü sipariş geçmişi sebebiyle gizliye çekildi: {menu.Name}", UserEmail = User.Identity?.Name });
                _context.SaveChanges();

                TempData["Success"] = "Geçmiş siparişleri olduğu için tamamen silinemedi, ancak müşterilerin görünümünden başarıyla KALDIRILDI.";
                return RedirectToAction(nameof(Index));
            }

            // Siparişi yoksa acıma, kökünden sil
            _context.Logs.Add(new DZGNCatering.Models.Log { Message = $"Menü sistemden kalıcı olarak silindi: {menu.Name}", UserEmail = User.Identity?.Name });

            _context.Menus.Remove(menu);
            _context.SaveChanges();

            TempData["Success"] = "Menü başarıyla sistemden kalıcı olarak kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }
    }
}