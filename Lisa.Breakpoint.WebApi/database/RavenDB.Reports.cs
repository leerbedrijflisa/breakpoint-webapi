using Raven.Abstractions.Data;
using Raven.Client.Linq;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lisa.Breakpoint.WebApi
{
    public partial class RavenDB
    {
        public IEnumerable<Report> GetAllReports(string organizationSlug, string projectSlug, IEnumerable<Filter> filters)
        {
            if (string.IsNullOrWhiteSpace(organizationSlug) || string.IsNullOrWhiteSpace(projectSlug))
            {
                return new List<Report>();
            }

            IQueryable<Report> rList = session.Query<Report>().Where(r => r.Organization == organizationSlug && r.Project == projectSlug );
            IEnumerable<Report> reports;

            if (filters.Any())
            {
                rList = rList.ApplyFilters(filters.ToArray());
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

        public Report GetReport(int id)
        {
            return session.Load<Report>(id);
        }

        public Report PostReport(ReportPost report, string organization, string project)
        {
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
            
            session.Store(reportEntity);

            string reportId = session.Advanced.GetDocumentId(reportEntity);
            reportEntity.Number = reportId.Split('/').Last();
            reportEntity.Reported = DateTime.Now;

            AddPlatforms(reportEntity.Organization, reportEntity.Platforms);

            session.SaveChanges();

            return reportEntity;
        }

        public Report PatchReport(int id, Report patchedReport)
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
                            _documentStore.DatabaseCommands.Patch("reports/" + id, new[] { patchRequest });
                        }
                        else if (newVal is Enum)
                        {
                            var patchRequest = new PatchRequest()
                            {
                                Name = propertyInfo.Name,
                                Type = PatchCommandType.Set,
                                Value = newVal.ToString()
                            };
                            _documentStore.DatabaseCommands.Patch("reports/" + id, new[] { patchRequest });
                        }
                        else
                        {
                            var patchRequest = new PatchRequest()
                            {
                                Name = propertyInfo.Name,
                                Type = PatchCommandType.Set,
                                Value = RavenJObject.FromObject((AssignedTo) newVal)
                            };
                            _documentStore.DatabaseCommands.Patch("reports/" + id, new[] { patchRequest });
                        }
                        }
                }
            }
            return session.Load<Report>(id);
        }

        public void DeleteReport(int id)
        {
            Report report = session.Load<Report>(id);
            session.Delete(report);
            session.SaveChanges();
        }
    }
}