using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    // REVIEW: Shouldn't the route be /version ?
    [Route("versions")]
    public class VersionController : Controller
    {
        [HttpGet]
        public string Get()
        {
            return "1.0.0-alpha-3";
        }
    }
}