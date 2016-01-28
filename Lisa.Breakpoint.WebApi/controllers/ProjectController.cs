using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Route("projects")]
    [Authorize("Bearer")]
    public class ProjectController : Controller
    {
        public ProjectController(RavenDB db)
        {
            _db = db;
            ErrorHandler.Clear();
        }

        [HttpGet("{organizationSlug}")]
        public IActionResult GetAll(string organizationSlug)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            var projects = _db.GetAllProjects(organizationSlug);

            return new HttpOkObjectResult(projects);
        }

        [HttpGet("{organizationSlug}/{projectSlug}", Name = "SingleProject")]
        public IActionResult Get(string organizationSlug, string projectSlug)
        {
            if (string.IsNullOrWhiteSpace(organizationSlug) || string.IsNullOrWhiteSpace(projectSlug))
            {
                return new HttpNotFoundResult();
            }

            var project = _db.GetProject(organizationSlug, projectSlug, _user.Name);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(project);
        }

        [HttpPost("{organizationSlug}")]
        public IActionResult Post(string organizationSlug, [FromBody] ProjectPost project)
        {
            ProjectValidator validator = new ProjectValidator(_db);
            List<Error> errors = new List<Error>();

            if (!_db.OrganizationExists(organizationSlug))
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new UnprocessableEntityObjectResult(ErrorHandler.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            var resource = new ResourceParameters
            {
                OrganizationSlug = organizationSlug,
                UserName = _user.Name
            };

            errors.AddRange(validator.ValidatePost(resource, project));

            if (errors.Any())
            {
                return new UnprocessableEntityObjectResult(errors);
            }

            var postedProject = _db.PostProject(project, organizationSlug);

            string location = Url.RouteUrl("SingleProject", new { organizationSlug = postedProject.Organization, projectSlug = postedProject.Slug }, Request.Scheme);
            return new CreatedResult(location, postedProject);
        }

        [HttpPatch("{organizationSlug}/{projectSlug}")]
        public IActionResult Patch(string organizationSlug, string projectSlug, [FromBody] IEnumerable<Patch> patches)
        {
            ProjectValidator validator = new ProjectValidator(_db);
            List<Error> errors = new List<Error>();

            if (patches == null)
            {
                return new BadRequestResult();
            }

            var project = _db.GetProject(organizationSlug, projectSlug, _user.Name);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }

            var resource = new ResourceParameters
            {
                OrganizationSlug = organizationSlug,
                ProjectSlug = projectSlug,
                UserName = _user.Name
            };

            errors.AddRange(validator.ValidatePatches(resource, patches));

            int projectNumber = int.Parse(project.Number);
            
            if (errors.Any())
            {
                return new UnprocessableEntityObjectResult(errors);
            }

            _db.Patch<Project>(projectNumber, patches);
            return new HttpOkObjectResult(_db.GetProject(organizationSlug, projectSlug, _user.Name));

        }

        [HttpDelete("{organizationSlug}/{projectSlug}")]
        public IActionResult Delete(string organizationSlug, string projectSlug)
        {
            if (_db.GetProject(organizationSlug, projectSlug, _user.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteProject(organizationSlug, projectSlug);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
        private IIdentity _user { get { return HttpContext.User.Identity; } }
    }
}