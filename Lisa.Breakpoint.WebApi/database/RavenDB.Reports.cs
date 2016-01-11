using Lisa.Breakpoint.WebApi.Models;
using Lisa.Breakpoint.WebApi.utils;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Linq;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lisa.Breakpoint.WebApi.database
{
    public partial class RavenDB
    {
        public IList<Report> GetAllReports(string organizationSlug, string projectSlug, string userName, IEnumerable<Filter> filters)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                IQueryable<Report> rList = session.Query<Report>().Where(r => r.Organization == organizationSlug && r.Project == projectSlug );
                IList<Report> reports;

                if (filters.Any())
                {
                    rList = rList.ApplyFilters(filters.ToArray());
                }

                if (ErrorHandler.HasErrors)
                {
                    return null;
                }

                // First, cast the ravenDB queryable to a regular list,
                // so it can be reconverted into a queryable that supports custom comparers to order the dataset as desired
                reports = rList.ToList().AsQueryable()
                    .OrderBy(x => x, new ReportComparer())
                    .ThenByDescending(r => r.Reported.Date)
                    .ThenBy(r => r.Reported.TimeOfDay)
                    .ToList();

                return reports;
            }
        }

        public Report GetReport(int id)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                return session.Load<Report>(id);
            }
        }

        public Report PostReport(ReportPost report, string organization, string project)
        {
            Project projectVersion = GetProject(organization, project, report.Reporter);

            if (projectVersion.CurrentVersion != report.Version)
            {
                var patches = new Patch[] {
                    new Patch() {
                        Action = "replace",
                        Field = "CurrentVersion",
                        Value = report.Version
                    }
                };

                Patch<Project>(int.Parse(projectVersion.Number), patches);
            }

            if (report.Platforms == null)
            {
                report.Platforms = new List<string>();
            }

            if (report.Platforms.Count == 0)
            {
                report.Platforms.Add("Not specified");
            }

            if (!Priorities.List.Contains(report.Priority))
            {
                ErrorHandler.Add(Priorities.InvalidValueError);
            }

            if (!Statuses.List.Contains(report.Status))
            {
                ErrorHandler.Add(Statuses.InvalidValueError);
            }


            if (ErrorHandler.HasErrors)
            {
                return null;
            }

            var reportEntity = new Report()
            {
                Title = report.Title,
                Organization = organization,
                Project = project,
                StepByStep = report.StepByStep,
                Expectation = report.Expectation,
                WhatHappened = report.WhatHappened,
                Reporter = report.Reporter,
                Status = report.Status,
                Priority = report.Priority,
                Version = report.Version,
                AssignedTo = report.AssignedTo,
                Platforms = report.Platforms ?? new List<string>(),
                Comments = new List<Comment>() // Add comments as new list so there's no null value in the database, even though comments aren't supported yet
            };

            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                session.Store(reportEntity);

                string reportId = session.Advanced.GetDocumentId(reportEntity);
                reportEntity.Number = reportId.Split('/').Last();
                reportEntity.Reported = DateTime.Now;

                AddPlatforms(reportEntity.Organization, reportEntity.Platforms);

                session.SaveChanges();

                return reportEntity;
            }
        }

        public Report PatchReport(int id, Report patchedReport)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                Report report = session.Load<Report>(id);

                foreach (PropertyInfo propertyInfo in report.GetType().GetProperties())
                {
                    var newVal = patchedReport.GetType().GetProperty(propertyInfo.Name).GetValue(patchedReport, null);

                    if (propertyInfo.Name != "Reported")
                    {
                        if (newVal != null)
                        {
                            if (newVal is string)
                            {
                                var patchRequest = new PatchRequest()
                                {
                                    Name = propertyInfo.Name,
                                    Type = PatchCommandType.Set,
                                    Value = newVal.ToString()
                                };
                                documentStore.DatabaseCommands.Patch("reports/" + id, new[] { patchRequest });
                            }
                            else if (newVal is Enum)
                            {
                                var patchRequest = new PatchRequest()
                                {
                                    Name = propertyInfo.Name,
                                    Type = PatchCommandType.Set,
                                    Value = newVal.ToString()
                                };
                                documentStore.DatabaseCommands.Patch("reports/" + id, new[] { patchRequest });
                            }
                            else
                            {
                                var patchRequest = new PatchRequest()
                                {
                                    Name = propertyInfo.Name,
                                    Type = PatchCommandType.Set,
                                    Value = RavenJObject.FromObject((AssignedTo) newVal)
                                };
                                documentStore.DatabaseCommands.Patch("reports/" + id, new[] { patchRequest });
                            }
                         }
                    }
                }
                return session.Load<Report>(id);
            }
        }

        public void DeleteReport(int id)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                Report report = session.Load<Report>(id);
                session.Delete(report);
                session.SaveChanges();
            }
        }
    }
}