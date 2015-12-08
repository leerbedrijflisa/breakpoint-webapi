﻿using Lisa.Breakpoint.WebApi.database;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    [Route("platforms")]
    public class PlatformController
    {
        public PlatformController(RavenDB db)
        {
            _db = db;
        }

        [HttpGet("{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult Get(string organizationSlug)
        {
            return new HttpOkObjectResult(_db.GetOrganization(organizationSlug).Platforms);
        }

        private RavenDB _db;
    }
}
