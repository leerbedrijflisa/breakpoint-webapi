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

            if (!Db.OrganizationExists(organizationSlug))
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
            {
                if (ErrorList.FromModelState(ModelState))
                {
                    return new UnprocessableEntityObjectResult(ErrorList.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            var resource = new ResourceParameters
            {
                OrganizationSlug = organizationSlug,
                UserName = CurrentUser.Name
            };

            ErrorList.FromValidator(validator.ValidatePost(resource, project));

            if (ErrorList.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            var postedProject = Db.PostProject(project, organizationSlug);

            string location = Url.RouteUrl("SingleProject", new { organizationSlug = postedProject.Organization, projectSlug = postedProject.Slug }, Request.Scheme);
            return new CreatedResult(location, postedProject);
        }

        [HttpPatch("{organizationSlug}/{projectSlug}")]
        public IActionResult Patch(string organizationSlug, string projectSlug, [FromBody] IEnumerable<Patch> patches)
        {
            ProjectValidator validator = new ProjectValidator(Db);

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

            ErrorList.FromValidator(validator.ValidatePatches(resource, patches));

            int projectNumber = int.Parse(project.Number);
            
            if (ErrorList.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorList.Errors);
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