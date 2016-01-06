using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;

namespace Lisa.Breakpoint.WebApi.controllers
{
    [Route("values")]
    public class ValueController
    {
        [HttpGet("priorities")]
        public IActionResult Get()
        {
            return new HttpOkObjectResult(Priorities.List);
        }

        [HttpGet("project-roles")]
        public IActionResult ProjectRoles()
        {
            return new HttpOkObjectResult(new { manager = "Project manager", developer = "Developer", tester = "Tester" });
        }
    }
}
