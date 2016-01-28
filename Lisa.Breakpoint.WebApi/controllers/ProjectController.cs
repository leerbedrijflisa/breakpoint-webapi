using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Lisa.Breakpoint.WebApi
{
    [Route("projects")]
    [Authorize("Bearer")]
    public class ProjectController : BaseController
    {
        public ProjectController(RavenDB db) 
            : base(db)
        {
        }

        [HttpGet("{organizationSlug}")]
        public IActionResult GetAll(string organizationSlug)
        {
            if (Db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            var projects = Db.GetAllProjects(organizationSlug);

            return new HttpOkObjectResult(projects);
        }

        [HttpGet("{organizationSlug}/{projectSlug}", Name = "SingleProject")]
        public IActionResult Get(string organizationSlug, string projectSlug)
        {
            if (string.IsNullOrWhiteSpace(organizationSlug) || string.IsNullOrWhiteSpace(projectSlug))
            {
                return new HttpNotFoundResult();
            }

            var project = Db.GetProject(organizationSlug, projectSlug, CurrentUser.Name);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(project);
        }

        [HttpPost("{organizationSlug}")]
        public IActionResult Post(string organizationSlug, [FromBody] ProjectPost project)
        {
            ProjectValidator validator = new ProjectValidator(Db);
            List<Error> errors = new List<Error>();

            if (!Db.OrganizationExists(organizationSlug))
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
                UserName = CurrentUser.Name
            };

            errors.AddRange(validator.ValidatePost(resource, project));

            if (errors.Any())
            {
                return new UnprocessableEntityObjectResult(errors);
            }

            var postedProject = Db.PostProject(project, organizationSlug);

            string location = Url.RouteUrl("SingleProject", new { organizationSlug = postedProject.Organization, projectSlug = postedProject.Slug }, Request.Scheme);
            return new CreatedResult(location, postedProject);
        }

        [HttpPatch("{organizationSlug}/{projectSlug}")]
        public IActionResult Patch(string organizationSlug, string projectSlug, [FromBody] IEnumerable<Patch> patches)
        {
            ProjectValidator validator = new ProjectValidator(Db);
            List<Error> errors = new List<Error>();

            if (patches == null)
            {
                return new BadRequestResult();
            }

            var project = Db.GetProject(organizationSlug, projectSlug, CurrentUser.Name);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }

            var resource = new ResourceParameters
            {
                OrganizationSlug = organizationSlug,
                ProjectSlug = projectSlug,
                UserName = CurrentUser.Name
            };

            errors.AddRange(validator.ValidatePatches(resource, patches));

            int projectNumber = int.Parse(project.Number);
            
            if (errors.Any())
            {
                return new UnprocessableEntityObjectResult(errors);
            }

            Db.Patch<Project>(projectNumber, patches);
            return new HttpOkObjectResult(Db.GetProject(organizationSlug, projectSlug, CurrentUser.Name));

        }

        [HttpDelete("{organizationSlug}/{projectSlug}")]
        public IActionResult Delete(string organizationSlug, string projectSlug)
        {
            if (Db.GetProject(organizationSlug, projectSlug, CurrentUser.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            Db.DeleteProject(organizationSlug, projectSlug);

            return new HttpStatusCodeResult(204);
        }
    }
}