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

            Allow<string>("name", organization.Name, new Action<string, object>[] { OrganizationNameIsUnique, NameCanBeSlug });
            Allow<string[]>("members", organization.Members.ToArray(), new Action<string[], object>[] { OrganizationHasMembers, MembersAreUser });

            return Errors;
        }

        #region Patch validation
        private void MemberIsUser(string value, dynamic parameters)
        {
            if (!Db.UserExists(value))
            {
                Errors.Add(new Error(1401, new { UserName = value}));
            }
        }

        private void MemberIsFound(string value, dynamic parameters)
        {
            if (!Db.GetOrganization(ResourceParams.OrganizationSlug).Members.Any(m => m == value))
            {
                Errors.Add(new Error(1402, new { UserName = value }));
            }
        }

        private void MemberIsNotInOrganization(string value, dynamic parameters)
        {
            if (Db.GetOrganization(ResourceParams.OrganizationSlug).Members.Contains(value))
            {
                Errors.Add(new Error(1311, new { UserName = value }));
            }
        }

        private void OrganizationRetainsMembers(string value, dynamic parameters)
        {
            var memberCount = Db.GetOrganization(ResourceParams.OrganizationSlug).Members.Count;
            
            // Use a counter to keep track of the amount of managers that have been deleted along all patches. This to prevent one patch deleting all managers at once.
            _membersDeleted++;
            if (memberCount <= 1 || _membersDeleted >= memberCount)
            {
                Errors.Add(new Error(1307));
            }
        }
        #endregion

        #region Post validation
        private void OrganizationNameIsUnique(string value, dynamic parameters)
        {
            if (Db.OrganizationExists(Db.ToUrlSlug(value)))
            {
                Errors.Add(new Error(1104, new { type = "organization", value = "name" }));
            }
        }

        private void OrganizationHasMembers(string[] value, dynamic parameters)
        {
            if (value.Count() < 1)
            {
                Errors.Add(new Error(1308));
            }
        }

        private void MembersAreUser(string[] value, dynamic parameters)
        {
            foreach (var member in value)
            {
                if (!Db.UserExists(member))
                {
                    Errors.Add(new Error(1401, new { UserName = member }));
                }
            }
        }

        private void NameCanBeSlug(string value, dynamic parameters)
        {
            if (string.IsNullOrWhiteSpace(Db.ToUrlSlug(value)))
            {
                Errors.Add(new Error(1213));
            }
        }
        #endregion

        private int _membersDeleted = 0;
    }
}
