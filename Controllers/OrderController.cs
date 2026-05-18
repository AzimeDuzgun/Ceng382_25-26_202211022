using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System;
using DZGNCatering.Data;
using DZGNCatering.Models;

namespace DZGNCatering.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // ==================== SİPARİŞ VERME EKRANI (GET) ====================
        [HttpGet]
        public IActionResult Create(int menuId)
        {
            var menu = _context.Menus.Include(m => m.Caretaker).FirstOrDefault(m => m.Id == menuId);
            if (menu == null) return NotFound();

            var order = new Order
            {
                MenuId = menuId,
                Menu = menu
            };
            return View(order);
        }

        // ==================== SİPARİŞİ KAYDETME (POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Order order)
        {
            var menu = _context.Menus.FirstOrDefault(m => m.Id == order.MenuId);
            if (menu == null) return NotFound();

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            order.UserId = int.Parse(userIdString);
            order.TotalPrice = menu.Price * order.Quantity;

            ModelState.Remove("User");
            ModelState.Remove("Menu");

            if (ModelState.IsValid)
            {
                _context.Orders.Add(order);

                // KUSURSUZ LOG MOTORU
                _context.Logs.Add(new DZGNCatering.Models.Log
                {
                    Message = $"{order.Quantity} adet {menu.Name} siparişi verildi. Toplam: {order.TotalPrice:C2}",
                    UserEmail = User.Identity.Name
                });
                // EMAIL SİMÜLASYONU: Sistem iki tarafa da mail fırlatmış gibi log üretiyor
                _context.Logs.Add(new DZGNCatering.Models.Log
                {
                    Message = $"[E-POSTA BİLDİRİMİ] Müşteriye ({User.Identity.Name}) sipariş onay maili gönderildi.",
                    UserEmail = "system@dzgn.com"
                });

                _context.Logs.Add(new DZGNCatering.Models.Log
                {
                    Message = $"[E-POSTA BİLDİRİMİ] Yemek Firmasına ({menu.Caretaker?.FullName}) yeni sipariş bildirim maili gönderildi.",
                    UserEmail = "system@dzgn.com"
                });
                _context.SaveChanges();
                return RedirectToAction(nameof(MyOrders));
            }

            order.Menu = menu;
            return View(order);
        }

        // ==================== MÜŞTERİ SİPARİŞ GEÇMİŞİ ====================
        public IActionResult MyOrders()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userId = int.Parse(userIdString);

            var myOrders = _context.Orders
                .Include(o => o.Menu)
                .ThenInclude(m => m.Caretaker)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(myOrders);
        }

        // ==================== FİRMA PANELİ VE İSTATİSTİKLER ====================
        [Authorize(Roles = "Caretaker")]
        public IActionResult CompanyOrders()
        {
            var companyIdString = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var companyId = int.Parse(companyIdString);

            var incomingOrders = _context.Orders
                .Include(o => o.Menu)
                .Include(o => o.User)
                .Where(o => o.Menu.CaretakerId == companyId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            ViewBag.TotalEarnings = incomingOrders.Where(o => o.Status != OrderStatus.IptalEdildi).Sum(o => o.TotalPrice);
            ViewBag.TotalOrders = incomingOrders.Count;
            ViewBag.PendingOrders = incomingOrders.Count(o => o.Status == OrderStatus.Beklemede);

            return View(incomingOrders);
        }

        // ==================== SİPARİŞ DURUM GÜNCELLEME ====================
        [HttpPost]
        [Authorize(Roles = "Caretaker")]
        public IActionResult UpdateStatus(int orderId, OrderStatus status)
        {
            var order = _context.Orders.Include(o => o.Menu).FirstOrDefault(o => o.Id == orderId);
            var companyIdString = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var companyId = int.Parse(companyIdString);

            if (order == null || order.Menu.CaretakerId != companyId) return Forbid();

            order.Status = status;
            _context.SaveChanges();

            return RedirectToAction(nameof(CompanyOrders));
        }

        // ==================== MÜŞTERİ: YORUM YAPMA EKRANI (GET) ====================
        [HttpGet]
        public IActionResult LeaveReview(int orderId)
        {
            var order = _context.Orders.Include(o => o.Menu).FirstOrDefault(o => o.Id == orderId);
            if (order == null) return NotFound();

            // HOCANIN İSTEDİĞİ GÜVENLİK KURALI: Teslim edilmemişse yorum yapılamaz!
            if (order.Status != OrderStatus.TeslimEdildi)
                return BadRequest("Yalnızca teslim edilen siparişlere yorum yapabilirsiniz.");

            var review = new Review { OrderId = orderId, Order = order };
            return View(review);
        }

        // ==================== MÜŞTERİ: YORUMU KAYDETME (POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LeaveReview(Review review)
        {
            var order = _context.Orders.Include(o => o.Menu).FirstOrDefault(o => o.Id == review.OrderId);
            if (order == null) return NotFound();

            ModelState.Remove("Order");

            if (ModelState.IsValid)
            {
                _context.Reviews.Add(review);

                // Sisteme log atıyoruz
                _context.Logs.Add(new DZGNCatering.Models.Log
                {
                    Message = $"Müşteri, {order.Menu.Name} siparişi için yorum bıraktı. Menü: {review.MenuRating}/5, Firma: {review.CaretakerRating}/5",
                    UserEmail = User.Identity.Name
                });

                _context.SaveChanges();
                return RedirectToAction(nameof(MyOrders)); // İş bitince siparişlerime dön
            }

            review.Order = order;
            return View(review);
        }

        // ==================== FATURA İNDİRME MOTORU (YENİ EKLENDİ) ====================
        public IActionResult DownloadInvoice(int id)
        {
            // Siparişi, menüyü, firmayı ve müşteriyi birbirine bağlayıp çekiyoruz
            var order = _context.Orders
                .Include(o => o.Menu).ThenInclude(m => m.Caretaker)
                .Include(o => o.User)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound("Sipariş bulunamadı.");

            // Temiz bir TXT faturası basıyoruz
            string fatura = $@"
===================================================
           DZGN CATERING - E-FATURA
===================================================
Fatura No: FAT-{order.Id}-{DateTime.Now.Year}
Düzenlenme Tarihi: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}

[ MÜŞTERİ BİLGİLERİ ]
Ad Soyad : {order.User?.FullName ?? "Bilinmiyor"}
E-Posta  : {order.User?.Email ?? "Bilinmiyor"}

[ FİRMA BİLGİLERİ ]
Firma Adı: {order.Menu?.Caretaker?.FullName ?? "DZGN Ortak Firma"}
Bölge    : {order.Menu?.Caretaker?.District ?? "Belirtilmemiş"}

[ SİPARİŞ DETAYI ]
Menü Adı    : {order.Menu?.Name ?? "Silinmiş Menü"}
Sipariş Adedi: {order.Quantity} Porsiyon
Birim Fiyat : {order.Menu?.Price.ToString("C2") ?? "0.00"}

---------------------------------------------------
TOPLAM ÖDENEN TUTAR: {((order.Quantity) * (order.Menu?.Price ?? 0)).ToString("C2")}
---------------------------------------------------

Bizi tercih ettiğiniz için teşekkür ederiz.
(Bu belge sistemsel olarak otomatik üretilmiştir.)
===================================================
";

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(fatura);

            // Kullanıcı butona bastığında "DZGN_Fatura_1.txt" diye tak diye iner
            return File(bytes, "text/plain", $"DZGN_Fatura_{order.Id}.txt");
        }

        // ==================== CANLI ÇAĞRI SİMÜLASYONU (YENİ EKLENDİ) ====================
        public IActionResult LiveCall(int id)
        {
            // Sadece firmanın adını ekranda göstermek için çekiyoruz
            var order = _context.Orders
                .Include(o => o.Menu).ThenInclude(m => m.Caretaker)
                .FirstOrDefault(o => o.Id == id);

            ViewBag.FirmaAdi = order?.Menu?.Caretaker?.FullName ?? "DZGN Ortak Firma";
            return View();
        }
    }
}