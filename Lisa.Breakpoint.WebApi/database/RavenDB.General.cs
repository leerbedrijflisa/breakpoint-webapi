using Raven.Client;
using System.Text.RegularExpressions;
using Raven.Abstractions.Data;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lisa.Breakpoint.WebApi
{
    public partial class RavenDB : IDisposable
    {
        public RavenDB(IDocumentStore docStore)
        {
            _documentStore = docStore;
            session = _documentStore.Initialize().OpenSession();
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
            _documentStore.DatabaseCommands.Patch(ravenId, patche);

            return true;
        }
        
        public string ToUrlSlug(string s)
        {
            return Regex.Replace(s, @"[^a-z0-9]+", "-", RegexOptions.IgnoreCase)
                .Trim(new char[] { '-' })
                .ToLower();
        }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    session.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                

                // Note disposing has been done.
                _disposed = true;

            }
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

        private readonly IDocumentStore _documentStore;
        private readonly IDocumentSession session;
        private bool _disposed;
    }
}