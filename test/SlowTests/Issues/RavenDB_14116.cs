﻿using FastTests;
using Orders;
using Raven.Client.Documents.Indexes;
using System.Linq;
using Xunit;

namespace SlowTests.Issues
{
    public class RavenDB_14116 : RavenTestBase
    {
        private class Companies_ByPhone : AbstractIndexCreationTask<Company>
        {
            public Companies_ByPhone()
            {
                Map = companies => from c in companies
                                   select new
                                   {
                                       c.Phone
                                   };
            }
        }

        [Fact]
        public void ShouldIndexAllDocuments()
        {
            using (var store = GetDocumentStore())
            {
                new Companies_ByPhone().Execute(store);

                using (var commands = store.Commands())
                {
                    commands.Put(
                        "companies/1",
                        null, new
                        {
                            Name = "HR",
                            Phone = "Hadera"
                        },
                        new System.Collections.Generic.Dictionary<string, object>
                        {
                            { Raven.Client.Constants.Documents.Metadata.Collection, "Companies" }
                        });

                    commands.Put(
                        "companies/2",
                        null, new
                        {
                            Name = "CF"
                        },
                        new System.Collections.Generic.Dictionary<string, object>
                        {
                            { Raven.Client.Constants.Documents.Metadata.Collection, "Companies" }
                        });
                }

                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var companies = session
                        .Advanced
                        .DocumentQuery<Company>()
                        .Statistics(out var stats1)
                        .Not
                        .WhereExists(x => x.Phone)
                        .ToList();

                    Assert.Equal(1, companies.Count);
                    Assert.Equal("CF", companies[0].Name);

                    companies = session
                        .Advanced
                        .DocumentQuery<Company>(stats1.IndexName)
                        .Statistics(out var stats2)
                        .ToList();

                    Assert.Equal(2, companies.Count);
                }

                using (var session = store.OpenSession())
                {
                    var companies = session
                        .Advanced
                        .DocumentQuery<Company, Companies_ByPhone>()
                        .Not
                        .WhereExists(x => x.Phone)
                        .ToList();

                    Assert.Equal(1, companies.Count);
                    Assert.Equal("CF", companies[0].Name);

                    companies = session
                        .Advanced
                        .DocumentQuery<Company, Companies_ByPhone>()
                        .ToList();

                    Assert.Equal(2, companies.Count);
                }
            }
        }
    }
}
