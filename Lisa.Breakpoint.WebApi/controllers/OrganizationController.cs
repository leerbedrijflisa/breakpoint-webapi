using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Route("organizations")]
    public class OrganizationController : Controller
    {
        public OrganizationController(RavenDB db)
        {
            _db = db;
        }
        
        [HttpGet]
        [Authorize("Bearer")]
        public IActionResult GetAll()
        {
            _user = HttpContext.User.Identity;

            var organizations = _db.GetAllOrganizations(_user.Name);

            if (organizations == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organizations);
        }

        [HttpGet("members/{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult GetOrganizationMembers(string organizationSlug)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            var members = _db.GetOrganization(organizationSlug).Members;

            return new HttpOkObjectResult(members);
        }

        [HttpGet("members/new/{organizationSlug}/{projectSlug}")]
        [Authorize("Bearer")]
        public IActionResult GetMembersNotInProject(string organizationSlug, string projectSlug)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            var members = _db.GetMembersNotInProject(organizationSlug, projectSlug);

            return new HttpOkObjectResult(members);
        }

        [HttpGet("{organizationSlug}", Name = "organization")]
        [Authorize("Bearer")]
        public IActionResult Get(string organizationSlug)
        {
            var organization = _db.GetOrganization(organizationSlug);

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organization);
        }

        [HttpPost]
        [Authorize("Bearer")]
        public IActionResult Post([FromBody] Organization organization)
        {
            if (organization == null)
            {
                return new BadRequestResult();
            }

            organization.Slug = RavenDB._toUrlSlug(organization.Name);

            var postedOrganization = _db.PostOrganization(organization);

            if (postedOrganization != null)
            {
                string location = Url.RouteUrl("organization", new { organizationSlug = organization }, Request.Scheme);
                return new CreatedResult(location, postedOrganization);
            }
            else
            {
                return new HttpStatusCodeResult(422);
            }

        }

        [HttpPatch("{id}")]
        [Authorize("Bearer")]
        public IActionResult Patch(int id, Organization organization)
        {
            var patchedOrganization = _db.PatchOrganization(id, organization);

            return new HttpOkObjectResult(patchedOrganization);
        }

        [HttpDelete("{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult Delete(string organizationSlug)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteOrganization(organizationSlug);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
        private IIdentity _user;
    }
}