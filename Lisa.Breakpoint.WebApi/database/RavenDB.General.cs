using Lisa.Breakpoint.WebApi.Models;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lisa.Breakpoint.WebApi.database
{
    public partial class RavenDB 
    {
        private readonly IDocumentStore documentStore;

        public RavenDB(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public bool Patch<T>(int id, IEnumerable<Patch> patches)
        {
            var patchFields = patches.Select(p => p.Field);

            // Fail if patch contains fields not in object that's getting patched
            var properties = typeof(T).GetProperties();
            if (patchFields.Where(f => !properties.Select(p => p.Name).Contains(f)).Count() > 0)
            {
                return false;
            }

            // Patch to RavenDB, use type name + id as RavenDB id
            var ravenId = string.Format("{0}s/{1}", typeof(T).Name.ToLower(), id.ToString());
            documentStore.DatabaseCommands.Patch(ravenId, ToRavenPatch(patches));

            return true;
        }

        private PatchRequest[] ToRavenPatch(IEnumerable<Patch> patches)
        {
            var ravenPatches = new List<PatchRequest>();

            foreach (var patch in patches)
            {
                var p = new PatchRequest()
                {
                    Name = patch.Field,
                    Value = patch.Value as string != null ? patch.Value as string : RavenJToken.FromObject(patch.Value)
                };

                switch (patch.Action)
                {
                    case "replace":
                        p.Type = PatchCommandType.Set;
                        break;
                    case "add":
                        p.Type = PatchCommandType.Add;
                        break;
                    case "remove":
                        p.Type = PatchCommandType.Remove;
                        break;
                    default:
                        throw new ArgumentException();
                        break;
                }

                ravenPatches.Add(p);
            }

            return ravenPatches.ToArray();
        }
    }
}