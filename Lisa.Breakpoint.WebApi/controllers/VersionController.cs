﻿using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    [Route("version")]
    public class VersionController : Controller
    {
        [HttpGet]
        public string Get()
        {
            return "1.0.0-alpha-3";
        }
    }
}