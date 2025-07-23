using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using PerfumeShop.Web.Models;

namespace PerfumeShop.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public abstract class BaseAdminController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            Console.WriteLine("==== BaseAdminController.OnActionExecuting ====");
            
            // Überprüfen, ob der Benutzer authentifiziert ist und eine gültige Session hat
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            Console.WriteLine($"UserSession aus Session: {userSessionJson}");
            
            if (string.IsNullOrEmpty(userSessionJson))
            {
                Console.WriteLine("UserSession ist null oder leer, Umleitung zur Login-Seite");
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
                return;
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);
            Console.WriteLine($"Deserialisierte UserSession: IsAuthenticated={userSession?.IsAuthenticated}, IsAdmin={userSession?.IsAdmin}");
            
            if (userSession == null || !userSession.IsAuthenticated || !userSession.IsAdmin)
            {
                Console.WriteLine("Benutzer ist nicht authentifiziert oder kein Admin, Umleitung zur Login-Seite");
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
                return;
            }
            
            Console.WriteLine("Admin-Authentifizierung erfolgreich");
        }
    }
} 