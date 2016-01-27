using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    [Route("platforms")]
    public class PlatformController
    {
        public PlatformController(RavenDB db)
        {
            _db = db;
        }

        [HttpGet("{organizationSlug}")]
        public IActionResult Get(string organizationSlug)
        {
            var organization = _db.GetOrganization(organizationSlug);

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organization.Platforms);
        }

        private RavenDB _db;
    }
}
