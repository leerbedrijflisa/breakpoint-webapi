using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    [Route("platforms")]
    public class PlatformController : BaseController
    {
        public PlatformController(RavenDB db)
            : base (db)
        {
        }

        [HttpGet("{organizationSlug}")]
        public IActionResult Get(string organizationSlug)
        {
            var organization = Db.GetOrganization(organizationSlug);

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organization.Platforms);
        }
    }
}
