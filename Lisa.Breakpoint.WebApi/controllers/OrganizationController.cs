using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Route("organizations")]
    public class OrganizationController : Controller
    {
        public OrganizationController(RavenDB db)
        {
            _db = db;
            _user = HttpContext.User.Identity;
        }
        
        [HttpGet]
        [Authorize("Bearer")]
        public IActionResult GetAll()
        {

            if (_db.GetUser(_user.Name) == null)
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

        [HttpGet("members/{organizationSlug}")]
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
        public IActionResult Post([FromBody]Organization organization)
        {
            if (organization == null)
            {
                return new BadRequestResult();
            }

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

        //[HttpPatch("{id}")]
        //public IActionResult Patch(int id, Organization organization)
        //{
        //    var patchedOrganization = _db.PatchOrganization(id, organization);

        //    return new HttpOkObjectResult(patchedOrganization);
        //}

        [HttpPatch("{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult Patch(string organizationSlug, IEnumerable<Patch> patches)
        {
            var organization = _db.GetOrganization(organizationSlug);

            if (organization == null || patches == null)
            {
                return new HttpNotFoundResult();
            }

            // 422 Unprocessable Entity : The request was well-formed but was unable to be followed due to semantic errors
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
                    return new HttpStatusCodeResult(422);
                }
            }
            catch (Exception)
            {
                // Internal server error if RavenDB throws exceptions
                return new HttpStatusCodeResult(500);
            }
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
        private readonly IIdentity _user;
    }
}