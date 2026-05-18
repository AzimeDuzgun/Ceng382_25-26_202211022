#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims; // E-posta tespiti için eklendi
using System.Collections.Generic; // Dictionary kullanımı için eklendi
using DZGNCatering.Data;

namespace DZGNCatering.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? filterDistrict)
        {
            var menusQuery = _context.Menus.Include(m => m.Caretaker).Where(m => m.IsActive).AsQueryable();

            string currentDistrict = "Çankaya"; // Ziyaretçiler için varsayılan ilçe
            bool showLocationModal = false; // Pop-up başlangıçta kapalı

            // 1. KONTROL: Adam dropdown'dan veya pop-up'tan bir ilçe seçip yolladı mı?
            if (!string.IsNullOrEmpty(filterDistrict))
            {
                // Seçtiyse tarayıcı hafızasına (Session) yaz
                HttpContext.Session.SetString("SessionDistrict", filterDistrict);
                currentDistrict = filterDistrict;
            }
            else
            {
                // 2. KONTROL: Seçim yoksa hafızada (Session) önceden kalma bir ilçe var mı?
                var sessionDist = HttpContext.Session.GetString("SessionDistrict");
                if (!string.IsNullOrEmpty(sessionDist))
                {
                    currentDistrict = sessionDist;
                }
                else
                {
                    // 3. KONTROL: Hafızada YOK. Peki bu adam GİRİŞ YAPMIŞ biri mi?
                    if (User.Identity != null && User.Identity.IsAuthenticated)
                    {
                        // ================= KRİTİK DOKUNUŞ =================
                        // EĞER GİRİŞ YAPAN KİŞİ "Caretaker" (Firma) DEĞİLSE MODALI AÇ!
                        if (!User.IsInRole("Caretaker"))
                        {
                            showLocationModal = true;
                        }
                        // ==================================================

                        // Müşterinin (veya Firmanın) kayıt olurken girdiği ilçeyi bulup veritabanından çekelim ki harita çok alakasız bir yerde durmasın
                        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                        var loggedInUser = _context.Users.FirstOrDefault(u => u.Email == userEmail);

                        if (loggedInUser != null && !string.IsNullOrEmpty(loggedInUser.District))
                        {
                            currentDistrict = loggedInUser.District;
                        }
                    }
                }
            }

            // Seçilen ilçeye göre filtrele
            menusQuery = menusQuery.Where(m => m.Caretaker.District == currentDistrict);

            ViewBag.UserDistrict = currentDistrict;
            ViewBag.ShowLocationModal = showLocationModal;

            var menus = menusQuery.ToList();

            // Yorumları Çekme Kısmı
            ViewBag.RecentReviews = _context.Reviews
                .Include(r => r.Order).ThenInclude(o => o.Menu).ThenInclude(m => m.Caretaker)
                .Include(r => r.Order).ThenInclude(o => o.User)
                .OrderByDescending(r => r.CreatedAt)
                .Take(4)
                .ToList();

            // ==================== İSTEDİĞİN 1. ADIM: ANA EKRAN PUAN HESAPLAMA MOTORU BURAYA GÖMÜLDÜ ====================
            var menuIds = menus.Select(m => m.Id).ToList();
            var ratingsGroup = _context.Reviews
                .Where(r => menuIds.Contains(r.Order.MenuId))
                .GroupBy(r => r.Order.MenuId)
                .Select(g => new {
                    MenuId = g.Key,
                    Avg = g.Average(r => r.MenuRating),
                    Count = g.Count()
                }).ToList();

            // Index.cshtml sayfasından tık diye okuyabilmek için Dictionary yapılarına çevirip ViewBag'e atıyoruz
            ViewBag.MenuAverageRatings = ratingsGroup.ToDictionary(x => x.MenuId, x => x.Avg);
            ViewBag.MenuReviewCounts = ratingsGroup.ToDictionary(x => x.MenuId, x => x.Count);
            // ==========================================================================================================

            return View(menus);
        }

        // ==================== 1. ADIM: MENÜ DETAY VE YORUM MOTORU BURAYA EKLENDİ ====================
        [HttpGet]
        public IActionResult MenuDetails(int id)
        {
            // Menüyü ve firmasını çekiyoruz
            var menu = _context.Menus
                .Include(m => m.Caretaker)
                .FirstOrDefault(m => m.Id == id);

            if (menu == null) return NotFound("Aradığınız menü sistemde bulunamadı.");

            // Bu menüye ait siparişler üzerinden yapılmış yorumları, yorumu yapan müşterinin adıyla birlikte çekiyoruz
            var reviews = _context.Reviews
                .Include(r => r.Order).ThenInclude(o => o.User)
                .Where(r => r.Order.MenuId == id)
                .ToList();

            // Verileri arayüze fırlatıyoruz
            ViewBag.Reviews = reviews;

            // Ortalama puanı hesapla (Yorum yoksa 0 yazmasın diye kontrol ekledik)
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.MenuRating) : 0;

            return View(menu);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}