using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;

namespace PlayerCards.Controllers
{
    public class LanguageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        } 

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
          CookieRequestCultureProvider.DefaultCookieName,
          CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
          new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
      );

            return LocalRedirect(returnUrl); //  Stay on the same page
        }

    }


}
