using Raven.Client;
using System.Text.RegularExpressions;
using Raven.Abstractions.Data;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Lisa.Breakpoint.WebApi
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
            if (patchFields.Where(f => !properties
                    .Select(p => p.Name.ToLower())
                    .Contains(f.ToLower())).Count() > 0)
            {
                throw new ArgumentException();
            }

            // Patch to RavenDB, use type name + id as RavenDB id
            var ravenId = string.Format("{0}s/{1}", typeof(T).Name.ToLower(), id.ToString());
            var patche = ToRavenPatch(patches, properties.Select(p => p.Name).ToArray());
            documentStore.DatabaseCommands.Patch(ravenId, patche);

            return true;
        }
        
        public string ToUrlSlug(string s)
        {
            return Regex.Replace(s, @"[^a-z0-9]+", "-", RegexOptions.IgnoreCase)
                .Trim(new char[] { '-' })
                .ToLower();
        }
        
        private PatchRequest[] ToRavenPatch(IEnumerable<Patch> patches, string[] propertyNames)
        {
            var ravenPatches = new List<PatchRequest>();

            foreach (var patch in patches)
            {
                var p = new PatchRequest()
                {
                    Name = propertyNames.Single(n => n.ToLower() == patch.Field.ToLower()),
                    Value = patch.Value is string ? patch.Value as string : RavenJToken.Parse(patch.Value.ToString())
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
                }

                ravenPatches.Add(p);
            }

            return ravenPatches.ToArray();
        }
    }
}