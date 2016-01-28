using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi
{
    class OrganizationValidator : ValidatorBase<OrganizationPost>
    {
        public OrganizationValidator(RavenDB db)
            :base(db)
        {

        }

        public override IEnumerable<Error> ValidatePatches(ResourceParameters resource, IEnumerable<Patch> patches)
        {
            Errors = new List<Error>();
            ResourceParams = resource;

            foreach (var patch in patches)
            {
                Allow<string>(patch, "add", new Regex(@"^members$"), new Action<string, object>[] { MemberIsUser }, new Action<dynamic>[] { });
                Allow<string>(patch, "remove", new Regex(@"^members$"), new Action<string, object>[] { MemberIsFound, OrganizationRetainsMembers }, new Action<dynamic>[] { });
            }

            SetRemainingPatchError(patches);
            ResourceParams = null;
            return Errors;
        }

        public override IEnumerable<Error> ValidatePost(ResourceParameters resource, OrganizationPost organization)
        {
            Errors = new List<Error>();

            Allow<string>("name", organization.Name, new Action<string, object>[] { NameCanBeSlug });
            Allow<string[]>("members", organization.Members.ToArray(), new Action<string[], object>[] { OrganizationHasMembers, MembersAreUser });

            return Errors;
        }

        #region Patch validation
        private void MemberIsUser(string value, dynamic parameters)
        {
            if (!Db.UserExists(value))
            {
                //TODO: Add error user doesn't exist.
                Errors.Add(new Error(1));
            }
        }

        private void MemberIsFound(string value, dynamic parameters)
        {
            if (!Db.GetOrganization(ResourceParams.OrganizationSlug).Members.Any(m => m == value))
            {
                //TODO: Add error member trying to remove not found
                Errors.Add(new Error(1));
            }
        }

        private void OrganizationRetainsMembers(string value, dynamic parameters)
        {
            if (Db.GetOrganization(ResourceParams.OrganizationSlug).Members.Count <= 1)
            {
                //TODO: Add error organization can't have less than 1 member
                Errors.Add(new Error(1));
            }
        }
        #endregion

        #region Post validation
        private void OrganizationHasMembers(string[] value, dynamic parameters)
        {
            if (value.Count() < 1)
            {
                //TODO: Add error organization must have members
                Errors.Add(new Error(1));
            }
        }

        private void MembersAreUser(string[] value, dynamic parameters)
        {
            foreach (var member in value)
            {
                if (!Db.UserExists(member))
                {
                    //TODO: Add error user doesn't exist.
                    Errors.Add(new Error(1));
                }
            }
        }

        private void NameCanBeSlug(string value, dynamic parameters)
        {
            if (string.IsNullOrWhiteSpace(Db.ToUrlSlug(value)))
            {
                //TODO: Add error project name not valid
                Errors.Add(new Error(1));
            }
        }
        #endregion
    }
}
