using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Authorize("Bearer")]
    [Route("organizations")]
    public class OrganizationController : Controller
    {
        public OrganizationController(RavenDB db)
        {
            _db = db;
            ErrorHandler.Clear();
        }
        
        [HttpGet]
        public IActionResult GetAll()
        {
            if (_db.UserExists(_user.Name))
            {
                return new HttpNotFoundResult();
            }

            var organizations = _db.GetAllOrganizations(_user.Name);

            if (organizations == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organizations);
        }
        
        [HttpGet("{organizationSlug}", Name = "SingleOrganization")]
        public IActionResult Get(string organizationSlug)
        {
            var organization = _db.GetOrganization(organizationSlug);

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organization);
        }

        // REVIEW: Would it be better to create a separate MemberController?
        [HttpGet("members/{organizationSlug}")]
        public IActionResult GetOrganizationMembers(string organizationSlug)
        {
            var organization = _db.GetOrganization(organizationSlug);

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            var members = organization.Members;

            return new HttpOkObjectResult(members);
        }

        [HttpGet("members/new/{organizationSlug}/{projectSlug}")]
        public IActionResult GetMembersNotInProject(string organizationSlug, string projectSlug)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            if (_db.GetProject(organizationSlug, projectSlug, _user.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            var members = _db.GetMembersNotInProject(organizationSlug, projectSlug);

            return new HttpOkObjectResult(members);
        }

        [HttpPost]
        public IActionResult Post([FromBody] OrganizationPost organization)
        {
            OrganizationValidator validator = new OrganizationValidator(_db);
            List<Error> errors = new List<Error>();

            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new UnprocessableEntityObjectResult(ErrorHandler.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            errors.AddRange(validator.ValidatePost(organization));

            var postedOrganization = _db.PostOrganization(organization);

            if (errors.Any())
            {
                return new UnprocessableEntityObjectResult(errors);
            }

            string location = Url.RouteUrl("SingleOrganization", new { organizationSlug = postedOrganization.Slug }, Request.Scheme);
            return new CreatedResult(location, postedOrganization);
        }

        [HttpPatch("{organizationSlug}")]
        public IActionResult Patch(string organizationSlug, [FromBody] IEnumerable<Patch> patches)
        {
            OrganizationValidator validator = new OrganizationValidator(_db);
            List<Error> errors = new List<Error>();

            if (patches == null)
            {
                return new BadRequestResult();
            }

            var organization = _db.GetOrganization(organizationSlug);

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            var resource = new ResourceParameters
            {
                OrganizationSlug = organizationSlug
            };

            errors.AddRange(validator.ValidatePatches(resource, patches));

            var organizationNumber = int.Parse(organization.Number);

            // Patch Report to database
            if (!errors.Any())
            {
                _db.Patch<Organization>(organizationNumber, patches);
                return new HttpOkObjectResult(_db.GetOrganization(organizationSlug));
            }

            // TODO: Add error message once Patch Authorization / Validation is finished.
            return new UnprocessableEntityObjectResult(errors);
        }

        [HttpDelete("{organizationSlug}")]
        public IActionResult Delete(string organizationSlug)
        {
            if (!_db.OrganizationExists(organizationSlug))
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteOrganization(organizationSlug);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
        private IIdentity _user { get { return HttpContext.User.Identity; } }
    }
}