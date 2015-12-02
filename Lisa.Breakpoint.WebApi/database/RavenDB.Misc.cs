﻿using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Client;
using Lisa.Breakpoint.WebApi.Models;

namespace Lisa.Breakpoint.WebApi.database
{
    public partial class RavenDB
    {
        public void AddPlatforms(string organizationSlug, IEnumerable<string> platforms)
        {
            // todo: Have this method patch the platforms field in organizations through the standard patching system

            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                var organization = session.Query<Organization>().SingleOrDefault(m => m.Slug.Equals(organizationSlug));
                var organizationId = organization.Number;
                var existingPlatforms = organization.Platforms;

                var patches = new List<PatchRequest>();

                foreach (var platform in platforms.Where(p => !existingPlatforms.Contains(p)))
                {
                    patches.Add(new PatchRequest()
                    {
                        Name = "Platforms",
                        Type = PatchCommandType.Add,
                        Value = platform
                    });
                }

                documentStore.DatabaseCommands.Patch("organizations/" + organizationId, patches.ToArray());
            }
        }
    }
}
