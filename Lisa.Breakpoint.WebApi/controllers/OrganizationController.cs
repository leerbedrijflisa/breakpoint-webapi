using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public IActionResult Post([FromBody] OrganizationPost organization)
        {
            List<Error> errors = new List<Error>();

            if (organization == null)
            {
                return new BadRequestResult();
            }

            if (!ModelState.IsValid)
            {
                var modelStateErrors = ModelState.Select(m => m).Where(x => x.Value.Errors.Count > 0);
                foreach (var property in modelStateErrors)
                {
                    var propertyName = property.Key;
                    foreach (var error in property.Value.Errors)
                    {
                        if (error.Exception == null)
                        {
                            errors.Add(new Error(1101, new { field = propertyName }));
                        }
                        else
                        {
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(error.Exception.Message));
                        }
                    }
                }

                return new BadRequestObjectResult(errors);
            }

            var postedOrganization = _db.PostOrganization(organization);

            if (_db.Errors.Count() == 0)
            {
                string location = Url.RouteUrl("organization", new { organizationSlug = postedOrganization.Slug }, Request.Scheme);
                return new CreatedResult(location, postedOrganization);
            }
            
            return new UnprocessableEntityObjectResult(_db.Errors);
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
        private IIdentity _user;
    }
}