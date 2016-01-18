using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    // TODO: Apply Authorize-attribute to controller instead of each action separately.
    [Route("organizations")]
    public class OrganizationController : Controller
    {
        public OrganizationController(RavenDB db)
        {
            _db = db;
            ErrorHandler.Clear();
        }
        
        [HttpGet]
        [Authorize("Bearer")]
        public IActionResult GetAll()
        {
            _user = HttpContext.User.Identity;

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

        // REVIEW: Would it be better to create a separate MemberController?
        [HttpGet("members/{organizationSlug}")]
        [Authorize("Bearer")]
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
        [Authorize("Bearer")]
        public IActionResult GetMembersNotInProject(string organizationSlug, string projectSlug)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            // TODO: Return 404 when project doesn't exist.

            var members = _db.GetMembersNotInProject(organizationSlug, projectSlug);

            return new HttpOkObjectResult(members);
        }

        [HttpPost]
        [Authorize("Bearer")]
        public IActionResult Post([FromBody] OrganizationPost organization)
        {
            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new BadRequestObjectResult(ErrorHandler.FatalError);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            var postedOrganization = _db.PostOrganization(organization);

            if (ErrorHandler.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            string location = Url.RouteUrl("organization", new { organizationSlug = postedOrganization.Slug }, Request.Scheme);
            return new CreatedResult(location, postedOrganization);
        }

        [HttpPatch("{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult Patch(string organizationSlug, IEnumerable<Patch> patches)
        {
            if (patches == null)
            {
                return new BadRequestResult();
            }

            var organization = _db.GetOrganization(organizationSlug);

            if (organization == null)
            {
                return new HttpNotFoundResult();
            }

            int organizationNumber;
            if (!int.TryParse(organization.Number, out organizationNumber))
            {
                return new HttpStatusCodeResult(500);
            }

            // Patch Report to database
            try
            {
                if (_db.Patch<Organization>(organizationNumber, patches))
                {
                    return new HttpOkObjectResult(_db.GetOrganization(organizationSlug));
                }
                else
                {
                    // TODO: Return an error message to indicate why validation failed.
                    // REVIEWFEEDBACK: Known, needs patch validation to generate errors first.
                    return new HttpStatusCodeResult(422);
                }
            }
            catch (Exception)
            {
                // REVIEW: Isn't this what ASP.NET does automatically if you don't catch the exception?
                // Internal server error if RavenDB throws exceptions
                return new HttpStatusCodeResult(500);
            }
        }

        [HttpDelete("{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult Delete(string organizationSlug)
        {
            //TODO: delete all the project and reports if the organization gets deleted.
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