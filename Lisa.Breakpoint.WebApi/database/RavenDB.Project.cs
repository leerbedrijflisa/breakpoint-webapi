using Raven.Abstractions.Data;
using Raven.Client;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lisa.Breakpoint.WebApi
{
    public partial class RavenDB
    {
        public IEnumerable<Project> GetAllProjects(string organizationName)
        {
            if (string.IsNullOrWhiteSpace(organizationName))
            {
                return new List<Project>();
            }

            return session.Query<Project>()
                .Where(p => p.Organization == organizationName)
                .ToList();
        }

        public Project GetProject(string organizationSlug, string projectSlug, string userName)
        {
            if (string.IsNullOrWhiteSpace(organizationSlug) || string.IsNullOrWhiteSpace(projectSlug))
            {
                return null;
            }
            
            var project = session.Query<Project>()
                .Where(p => p.Organization == organizationSlug && p.Slug == projectSlug)
                .SingleOrDefault();

            if (project == null)
            {
                return null;
            }

            return project;
        }

        public Project PostProject(ProjectPost project, string organizationSlug)
        {
            var projectEntity = new Project()
            {
                Name = project.Name,
                CurrentVersion = project.CurrentVersion,
                Members = project.Members,
                Organization = organizationSlug,
                Slug = ToUrlSlug(project.Name)
            };

            session.Store(projectEntity);
            string projectId = session.Advanced.GetDocumentId(projectEntity);
            projectEntity.Number = projectId.Split('/').Last();

            session.SaveChanges();

            return projectEntity;
        }

        public Project PatchProject(int id, Project patchedProject)
        {
            Project project = session.Load<Project>(id);

            foreach (PropertyInfo propertyInfo in project.GetType().GetProperties())
            {
                var newVal = patchedProject.GetType().GetProperty(propertyInfo.Name).GetValue(patchedProject, null);

                if (newVal != null)
                {
                    var patchRequest = new PatchRequest()
                    {
                        Name = propertyInfo.Name,
                        Type = PatchCommandType.Add,
                        Value = newVal.ToString()
                    };
                    _documentStore.DatabaseCommands.Patch("projects/" + id, new[] { patchRequest });
                }
            }

            return project;
        }

        //public Project PatchProjectMembers(string organizationSlug, string projectSlug, TempMemberPatch patch)
        //{
        //    using (IDocumentSession session = documentStore.Initialize().OpenSession())
        //    {
        //        Project project = session.Query<Project>()
        //            .Where(p => p.Organization == organizationSlug && p.Slug == projectSlug)
        //            .SingleOrDefault();

        //        bool roleExist = false;

        //        string sender      = patch.Sender;
        //        string senderRole  = GetGroupFromUser(project.Organization, project.Slug, sender);
        //        int    senderLevel = 0;

        //        string type      = patch.Type;
        //        string role      = patch.Role;
        //        string member    = patch.Member;
        //        int    roleLevel = 0;

        //        IList<Group> groups = GetGroupsFromProject(project.Organization, project.Slug);

        //        foreach (Group group in groups)
        //        {
        //            if (group.Name == senderRole)
        //            {
        //                senderLevel = group.Level;
        //            }
        //            if (group.Name == role)
        //            {
        //                roleLevel = group.Level;
        //                roleExist = true;
        //            }
        //        }

        //        if (!roleExist)
        //        {
        //            return null;
        //        }

        //        if (senderLevel >= roleLevel)
        //        {
        //            IList<Member> members = project.Members;
        //            if (type == "add")
        //            {
        //                bool isInProject = false;
        //                Member newMember = new Member();

        //                newMember.Username = member;
        //                newMember.Role = role;

        //                foreach (var m in members)
        //                {
        //                    if (m.Username == newMember.Username)
        //                    {
        //                        isInProject = true;
        //                        break;
        //                    }
        //                }

        //                if (!isInProject)
        //                {
        //                    members.Add(newMember);
        //                }
        //            }
        //            else if (type == "remove")
        //            {
        //                Member newMember = new Member();

        //                for (int i = 0; i < members.Count; i++)
        //                {
        //                    if (members[i].Username == member)
        //                    {
        //                        members.RemoveAt(i);
        //                        break;
        //                    }
        //                }
        //            }
        //            else if (type == "update")
        //            {
        //                foreach (var m in members)
        //                {
        //                    if (m.Username == member)
        //                    {
        //                        m.Role = role;
        //                        break;
        //                    }
        //                }
        //            }
        //            project.Members = members;

        //            session.SaveChanges();
        //            return project;
        //        } else
        //        {
        //            return null;
        //        }
        //    }
        //}


        public void DeleteProject(string organizationSlug, string projectSlug)
        {
            Project project = session.Query<Project>().Where(p => p.Slug == projectSlug && p.Organization == organizationSlug).SingleOrDefault();

            List<Report> reports = session.Query<Report>().Where(r => r.Organization == organizationSlug && r.Project == projectSlug).ToList();

            foreach (var report in reports)
            {
                session.Delete(report);
            }
            session.Delete(project);
            session.SaveChanges();
        }

        public Project GetProjectByReport(int id, string username)
        {
            var report = session.Load<Report>(id);

            return session.Query<Project>()
                .Where(p => p.Organization == report.Organization && p.Slug == report.Project)
                .SingleOrDefault();
        }

        public bool ProjectExists(string organizationSlug, string projectSlug)
        {
            return session.Query<Organization>().Any(o => o.Slug == organizationSlug)
                && session.Query<Project>().Any(p => p.Slug == projectSlug && p.Organization == organizationSlug);
        }
    }
}