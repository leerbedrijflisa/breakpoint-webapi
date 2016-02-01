using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;

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
            if (!Db.UserExists(CurrentUser.Name))
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

            if (!ModelState.IsValid)
            {
                if (ErrorList.FromModelState(ModelState))
                {
                    return new UnprocessableEntityObjectResult(ErrorList.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            ErrorList.FromValidator(validator.ValidatePost(new ResourceParameters(), organization));

            var postedOrganization = Db.PostOrganization(organization);

            if (ErrorList.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            string location = Url.RouteUrl("SingleOrganization", new { organizationSlug = postedOrganization.Slug }, Request.Scheme);
            return new CreatedResult(location, postedOrganization);
        }

        [HttpPatch("{organizationSlug}")]
        public IActionResult Patch(string organizationSlug, [FromBody] IEnumerable<Patch> patches)
        {
            OrganizationValidator validator = new OrganizationValidator(Db);

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

            ErrorList.FromValidator(validator.ValidatePatches(resource, patches));

            var organizationNumber = int.Parse(organization.Number);

            // Patch Report to database
            if (ErrorList.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorList.Errors);
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