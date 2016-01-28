using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Lisa.Breakpoint.WebApi
{
    [Authorize("Bearer")]
    [Route("organizations")]
    public class OrganizationController : BaseController
    {
        public OrganizationController(RavenDB db)
            : base(db)
        {
        }
        
        [HttpGet]
        public IActionResult GetAll()
        {
            if (Db.UserExists(CurrentUser.Name))
            {
                return new HttpNotFoundResult();
            }

            var organizations = Db.GetAllOrganizations(CurrentUser.Name);

            if (organizations == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organizations);
        }
        
        [HttpGet("{organizationSlug}", Name = "SingleOrganization")]
        public IActionResult Get(string organizationSlug)
        {
            var organization = Db.GetOrganization(organizationSlug);

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
            var organization = Db.GetOrganization(organizationSlug);

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
            if (Db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            if (Db.GetProject(organizationSlug, projectSlug, CurrentUser.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            var members = Db.GetMembersNotInProject(organizationSlug, projectSlug);

            return new HttpOkObjectResult(members);
        }

        [HttpPost]
        public IActionResult Post([FromBody] OrganizationPost organization)
        {
            OrganizationValidator validator = new OrganizationValidator(Db);
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

            errors.AddRange(validator.ValidatePost(new ResourceParameters(), organization));

            var postedOrganization = Db.PostOrganization(organization);

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
            OrganizationValidator validator = new OrganizationValidator(Db);
            List<Error> errors = new List<Error>();

            if (patches == null)
            {
                return new BadRequestResult();
            }

            var organization = Db.GetOrganization(organizationSlug);

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
                return new UnprocessableEntityObjectResult(errors);
            }

            Db.Patch<Organization>(organizationNumber, patches);
            return new HttpOkObjectResult(Db.GetOrganization(organizationSlug));
        }

        [HttpDelete("{organizationSlug}")]
        public IActionResult Delete(string organizationSlug)
        {
            if (!Db.OrganizationExists(organizationSlug))
            {
                return new HttpNotFoundResult();
            }

            Db.DeleteOrganization(organizationSlug);

            return new HttpStatusCodeResult(204);
        }
    }
}