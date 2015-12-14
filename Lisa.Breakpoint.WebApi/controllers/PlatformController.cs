﻿using Lisa.Breakpoint.WebApi.database;
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
        public IActionResult Get(string organizationSlug)
        {
            if (string.IsNullOrWhiteSpace(organizationSlug))
            {
                return new BadRequestResult();
            }

            return new HttpOkObjectResult(_db.GetOrganization(organizationSlug).Platforms);
        }

        private RavenDB _db;
    }
}
