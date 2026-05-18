using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using DZGNCatering.Data;
using DZGNCatering.Models;

namespace DZGNCatering.Controllers
{
    [Authorize] // POLİSİ YUMUŞATTIK: Sadece "Giriş yapmış" olması kapıdan girmesi için yeterli.
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Logs()
        {
            // İÇERİDEKİ GİZLİ GÜVENLİK DUVARI: İçeri giren kişinin adında "Yönetici" geçmiyorsa çaktırmadan anasayfaya geri postala.
            // Bu sayede 404 AccessDenied sayfasına düşmek yerine temiz bir güvenlik önlemi almış oluyoruz.
            if (User.Identity == null || (!User.Identity.Name.Contains("Yönetici") && !User.IsInRole("Admin") && !User.IsInRole("2")))
            {
                return RedirectToAction("Index", "Home");
            }

            var systemLogs = _context.Logs.OrderByDescending(l => l.ActionDate).ToList();
            return View(systemLogs);
        }
    }
}