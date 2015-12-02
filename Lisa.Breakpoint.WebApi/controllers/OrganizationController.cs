using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;

namespace Lisa.Breakpoint.WebApi
{
    [Route("organizations")]
    public class OrganizationController : Controller
    {
        public OrganizationController(RavenDB db)
        {
            _db = db;
        }

        [HttpGet("{username}")]
        public IActionResult GetAll(string userName)
        {
            if (_db.GetUser(userName) == null)
            {
                return new HttpNotFoundResult();
            }

            var organizations = _db.GetAllOrganizations(userName);

            if (organizations == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organizations);
        }

        [HttpGet("members/{organization}")]
        public IActionResult GetOrganizationMembers(string organization)
        {
            if (_db.GetOrganization(organization) == null)
            {
                return new HttpNotFoundResult();
            }

            var members = _db.GetOrganizationMembers(organization);

            return new HttpOkObjectResult(members);
        }

        [HttpGet("members/new/{organization}/{project}")]
        public IActionResult GetMembersNotInProject(string organization, string project)
        {
            if (_db.GetOrganization(organization) == null)
            {
                return new HttpNotFoundResult();
            }

            var members = _db.GetMembersNotInProject(organization, project);

            return new HttpOkObjectResult(members);
        }

        [HttpGet("get/{organizationSlug}", Name = "organization")]
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
            } else
            {
                return new NoContentResult();
            }

        }

        //[HttpPatch("{id}")]
        //public IActionResult Patch(int id, Organization organization)
        //{
        //    var patchedOrganization = _db.PatchOrganization(id, organization);

        //    return new HttpOkObjectResult(patchedOrganization);
        //}

        [HttpPatch("{organizationSlug}")]
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

        [HttpDelete("{organization}")]
        public IActionResult Delete(string organization)
        {
            if (_db.GetOrganization(organization) == null)
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteOrganization(organization);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
    }
}