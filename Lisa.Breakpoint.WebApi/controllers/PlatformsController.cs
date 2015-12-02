using Lisa.Breakpoint.WebApi.database;
using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    [Route("platforms")]
    public class PlatformsController
    {
        public PlatformsController(RavenDB db)
        {
            _db = db;
        }

        [HttpGet("{organizationSlug")]
        public IActionResult Get(string organizationSlug)
        {
            return new HttpOkObjectResult(_db.GetOrganization(organizationSlug).Platforms);
        }

        private RavenDB _db;
    }
}
