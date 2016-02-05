using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Json.Linq;

namespace Lisa.Breakpoint.WebApi
{
    public partial class RavenDB
    {
        public void AddPlatforms(string organizationSlug, IEnumerable<string> platforms)
        {
            var organization = session.Query<Organization>().SingleOrDefault(m => m.Slug.Equals(organizationSlug));
            var organizationId = organization.Number;
            var existingPlatforms = organization.Platforms;
            if (existingPlatforms != null)
            {
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

                _documentStore.DatabaseCommands.Patch("organizations/" + organizationId, patches.ToArray());
            }
            else
            {
                var patches = new List<PatchRequest>();

                patches.Add(new PatchRequest()
                {
                    Name = "Platforms",
                    Type = PatchCommandType.Set,
                    Value = RavenJToken.FromObject(platforms)
                });

                _documentStore.DatabaseCommands.Patch("organizations/" + organizationId, patches.ToArray());
            }
        }
    }
}
