﻿using Raven.Abstractions.Data;
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

            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                return session.Query<Project>()
                    .Where(p => p.Organization == organizationName)
                    .ToList();
            }
        }

        // REVIEW: Why does this method only return projects for a specific user?
        public IEnumerable<Project> GetAllProjectsFromUser(string organizationName, string userName)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                return session.Query<Project>()
                    .Where(p => p.Organization == organizationName 
                        && p.Members.Select(m => m.Username).Contains(userName))
                    .ToList();
            }
        }

        // TODO: Make includeAllGroups a boolean.
        // REVIEW: Why does this method need the user name?
        public Project GetProject(string organizationSlug, string projectSlug, string userName, string includeAllGroups = "false")
        {
            if (string.IsNullOrWhiteSpace(organizationSlug) || string.IsNullOrWhiteSpace(projectSlug))
            {
                return null;
            }

            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                var filter = false;
                var project = session.Query<Project>()
                    .Where(p => p.Organization == organizationSlug && p.Slug == projectSlug)
                    .SingleOrDefault();

                if (project == null)
                {
                    return null;
                }

                // check the role (level) of the userName
                // manager = 3; developer = 2; tester = 1;
                // remove the roles you may not use
                if (includeAllGroups == "false")
                {
                    foreach (Member member in project.Members)
                    {
                        if (member.Username == userName)
                        {
                            var role  = member.Role;
                            if (!string.IsNullOrWhiteSpace(role))
                            {
                                int level = project.Groups
                                    .Where(g => g.Name == role)
                                    .Select(g => g.Level)
                                    .SingleOrDefault();

                                var groups = project.Groups.ToList();

                                groups.RemoveAll(g => g.Level > level);

                                project.Groups = groups;

                                filter = true;
                            }
                        }
                    }
                    if (!filter)
                    {
                        project.Groups = null;
                    }
                }

                return project;
            }
        }

        public IList<Group> GetGroupsFromProject(string organization, string projectSlug)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                var groups = session.Query<Project>()
                    .Where(p => p.Organization == organization && p.Slug == projectSlug)
                    .Select(p => p.Groups)
                    .SingleOrDefault();
                
                return groups;
            }
        }

        public Project PostProject(ProjectPost project, string organizationSlug)
        {
            var projectEntity = new Project()
            {
                Name = project.Name,
                CurrentVersion = project.CurrentVersion,
                Groups = project.Groups,
                Members = project.Members,
                Organization = organizationSlug,
                Slug = ToUrlSlug(project.Name)
            };

            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                // If there is already a duplicate project in the organization
                if (session.Query<Project>().Where(p => p.Organization == projectEntity.Organization && p.Slug == projectEntity.Slug).Any())
                {
                    ErrorHandler.Add(new Error(1102, new { type = "project", value = "name" }));
                }

                var organizationMembers = session.Query<Organization>().SingleOrDefault(o => o.Slug == projectEntity.Organization).Members;

                // Check if all project members are part of the organization
                foreach (var user in projectEntity.Members)
                {
                    if (!organizationMembers.Any(m => m == user.Username))
                    {
                        ErrorHandler.Add(new Error(1305, new { value = user.Username }));
                    }
                }

                if (ErrorHandler.HasErrors)
                {
                    return null;
                }

                session.Store(projectEntity);
                string projectId = session.Advanced.GetDocumentId(projectEntity);
                projectEntity.Number = projectId.Split('/').Last();

                session.SaveChanges();

                return projectEntity;
            }
        }

        public Project PatchProjectMembers(string organizationSlug, string projectSlug, TempMemberPatch patch)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                Project project = session.Query<Project>()
                    .Where(p => p.Organization == organizationSlug && p.Slug == projectSlug)
                    .SingleOrDefault();

                bool roleExist = false;

                string sender      = patch.Sender;
                string senderRole  = GetGroupFromUser(project.Organization, project.Slug, sender);
                int    senderLevel = 0;

                string type      = patch.Type;
                string role      = patch.Role;
                string member    = patch.Member;
                int    roleLevel = 0;

                IList<Group> groups = GetGroupsFromProject(project.Organization, project.Slug);

                foreach (Group group in groups)
                {
                    if (group.Name == senderRole)
                    {
                        senderLevel = group.Level;
                    }
                    if (group.Name == role)
                    {
                        roleLevel = group.Level;
                        roleExist = true;
                    }
                }

                if (!roleExist)
                {
                    return null;
                }

                if (senderLevel >= roleLevel)
                {
                    IList<Member> members = project.Members;
                    if (type == "add")
                    {
                        bool isInProject = false;
                        Member newMember = new Member();

                        newMember.Username = member;
                        newMember.Role = role;

                        foreach (var m in members)
                        {
                            if (m.Username == newMember.Username)
                            {
                                isInProject = true;
                                break;
                            }
                        }

                        if (!isInProject)
                        {
                            members.Add(newMember);
                        }
                    }
                    else if (type == "remove")
                    {
                        Member newMember = new Member();

                        for (int i = 0; i < members.Count; i++)
                        {
                            if (members[i].Username == member)
                            {
                                members.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    else if (type == "update")
                    {
                        foreach (var m in members)
                        {
                            if (m.Username == member)
                            {
                                m.Role = role;
                                break;
                            }
                        }
                    }
                    project.Members = members;

                    session.SaveChanges();
                    return project;
                } else
                {
                    return null;
                }
            }
        }

        public void DeleteProject(string organizationSlug, string projectSlug)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
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
        }

        public Project GetProjectByReport(int id, string username)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                var report = session.Load<Report>(id);

                return session.Query<Project>()
                    .Where(p => p.Organization == report.Organization && p.Slug == report.Project)
                    .SingleOrDefault();
            }
        }

        public bool ProjectExists(string organizationSlug, string projectSlug)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                return session.Query<Organization>().Any(o => o.Slug == organizationSlug)
                    && session.Query<Project>().Any(p => p.Slug == projectSlug && p.Organization == organizationSlug);
            }
        }
    }
}