using Microsoft.AspNetCore.Mvc;

namespace DeskSyndic.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() => View();
}
