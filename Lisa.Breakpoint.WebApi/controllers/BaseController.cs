using Microsoft.AspNet.Mvc;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    public abstract class BaseController : Controller
    {
        public BaseController(RavenDB db)
        {
            Db = db;
            ErrorList = new ErrorHandler();
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected override void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Db.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                

                // Note disposing has been done.
                _disposed = true;

                base.Dispose();
            }
        }

        protected RavenDB Db { get; private set; }
        protected IIdentity CurrentUser { get { return HttpContext.User.Identity; } }
        protected ErrorHandler ErrorList { get; private set; }
        private bool _disposed = false;
    }
}
