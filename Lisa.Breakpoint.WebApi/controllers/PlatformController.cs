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
            if (string.IsNullOrWhiteSpace(organizationSlug))
            {
                // REVIEW: Shouldn't this be a 404?
                return new BadRequestResult();
            }

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
