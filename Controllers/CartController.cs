using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DZGNCatering.Data;
using DZGNCatering.Models;
using DZGNCatering.Extensions; // Yazdığımız eklentiyi buraya çağırdık

namespace DZGNCatering.Controllers
{
    [Authorize(Roles = "Customer")] // Sadece müşteriler sepet kullanabilir
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // ================= SEPETİ GÖRÜNTÜLE =================
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        // ================= SEPETE EKLE =================
        [HttpPost]
        public IActionResult AddToCart(int menuId, int quantity = 1)
        {
            var menu = _context.Menus.FirstOrDefault(m => m.Id == menuId);
            if (menu == null || !menu.IsActive) return NotFound();

            // Sepeti Session'dan çek (Yoksa yeni oluştur)
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Bu ürün zaten sepette var mı?
            var existingItem = cart.FirstOrDefault(c => c.MenuId == menuId);

            if (existingItem != null)
            {
                // Varsa sadece adedini artır
                existingItem.Quantity += quantity;
            }
            else
            {
                // Yoksa yeni kalem olarak ekle
                cart.Add(new CartItem
                {
                    MenuId = menu.Id,
                    MenuName = menu.Name,
                    ImageUrl = string.IsNullOrEmpty(menu.ImageUrl) ? "/css/default-food.jpg" : menu.ImageUrl,
                    Price = menu.Price,
                    Quantity = quantity,
                    CaretakerId = menu.CaretakerId,
                    CaretakerName = menu.Caretaker?.FullName ?? "Bilinmeyen Firma"
                });
            }

            // Güncel sepeti tekrar Session'a kaydet
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            TempData["Success"] = $"{menu.Name} başarıyla sepete eklendi.";
            return RedirectToAction("Index", "Home"); // Ekledikten sonra ana sayfaya döner
        }

        // ================= SEPETTEN SİL =================
        [HttpPost]
        public IActionResult RemoveFromCart(int menuId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");
            if (cart != null)
            {
                var itemToRemove = cart.FirstOrDefault(c => c.MenuId == menuId);
                if (itemToRemove != null)
                {
                    cart.Remove(itemToRemove);
                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                    TempData["Success"] = "Ürün sepetten çıkarıldı.";
                }
            }
            return RedirectToAction("Index");
        }

        // ================= SEPETİ TEMİZLE =================
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            TempData["Success"] = "Sepet tamamen boşaltıldı.";
            return RedirectToAction("Index");
        }
        // ================= SİPARİŞİ TAMAMLA VE ÖDEME AL =================
        [HttpPost]
        public IActionResult Checkout(string deliveryAddress)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Sepetiniz boş, sipariş veremezsiniz!";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["Error"] = "Teslimat adresi girmek zorundasınız!";
                return RedirectToAction("Index");
            }

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = int.Parse(userIdString);

            // Sepetteki her farklı yemek için ayrı bir sipariş satırı (Order) oluşturuyoruz
            foreach (var item in cart)
            {
                var newOrder = new Order
                {
                    UserId = currentUserId,
                    MenuId = item.MenuId,
                    Quantity = item.Quantity,
                    TotalPrice = item.SubTotal,
                    DeliveryAddress = deliveryAddress,
                    Status = OrderStatus.Beklemede,
                    OrderDate = DateTime.Now
                };

                _context.Orders.Add(newOrder);
            }

            // Ödeme alındı ve siparişler onaya düştü logu
            _context.Logs.Add(new DZGNCatering.Models.Log { Message = "Sepetteki ürünler için 3D Secure simülasyonu ile ödeme alındı ve sipariş oluşturuldu.", UserEmail = User.Identity?.Name });

            _context.SaveChanges();

            // Müşterinin sepetini sıfırla
            HttpContext.Session.Remove("Cart");

            TempData["Success"] = "Ödemeniz başarıyla alındı ve siparişleriniz onaylandı!";

            // Siparişler sayfasına yolla (Sende bu sayfanın Controller/Action adı neyse ona göre düzelt)
            return RedirectToAction("MyOrders", "Order");
        }
    }
}