using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi.controllers
{
    [Route("values")]
    public class ValueController
    {
        [HttpGet("priorities")]
        public IActionResult Get()
        {
            return new HttpOkObjectResult(new {
                immediately = "Fix Immediately",
                beforeRelease = "Fix Before Release",
                nextRelease = "Fix For Next Release",
                whenever = "Fix Whenever"
            });
        }

        [HttpGet("project-roles")]
        public IActionResult ProjectRoles()
        {
            return new HttpOkObjectResult(new {
                manager = "Project manager",
                developer = "Developer",
                tester = "Tester"
            });
        }

        [HttpGet("statuses")]
        public IActionResult Statuses()
        {
            return new HttpOkObjectResult(new {
                open = "Open",
                @fixed = "Fixed",
                wontFix = "Won't Fix",
                wontFixApproved = "Won't Fix (Approved)",
                closed = "Closed"
            });
        }

        [HttpGet("organization-roles")]
        public IActionResult OrganizationRoles()
        {
            return new HttpOkObjectResult(new {
                manager = "Organization manager",
                member = "Member"
            });
        }
    }
}
