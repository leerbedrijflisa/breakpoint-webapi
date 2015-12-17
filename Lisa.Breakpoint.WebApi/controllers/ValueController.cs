using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;

namespace Lisa.Breakpoint.WebApi.controllers
{
    [Route("values")]
    public class ValueController
    {
        [HttpGet("priorities")]
        public IEnumerable<string> Get()
        {
            return Priorities.List;
        }
    }
}
