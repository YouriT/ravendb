﻿// -----------------------------------------------------------------------
//  <copyright file="Crud.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FastTests;
using Raven.Client.Data;
using Raven.Client.Indexing;
using Raven.Client.Operations.Databases;
using Raven.Client.Operations.Databases.Indexes;
using SlowTests.Core.Utils.Indexes;
using Sparrow.Json;
using Xunit;

using User = SlowTests.Core.Utils.Entities.User;

namespace SlowTests.Core.Commands
{
    public class Indexes : RavenNewTestBase
    {
        [Fact]
        public async Task CanPutUpdateAndDeleteMapIndex()
        {
            using (var store = GetDocumentStore())
            {
                const string usersByname = "users/byName";

                using (var commands = store.Commands())
                {
                    await commands.PutAsync("users/1", null, new User { Name = "testname" }, null);
                }

                await store.Admin.SendAsync(new PutIndexOperation(usersByname, new IndexDefinition
                {
                    Maps = { "from user in docs.Users select new { user.Name }" }
                }));

                var index = await store.Admin.SendAsync(new GetIndexOperation(usersByname));
                Assert.Equal(usersByname, index.Name);

                var indexes = await store.Admin.SendAsync(new GetIndexesOperation(0, 5));
                Assert.Equal(1, indexes.Length);

                await store.Admin.SendAsync(new DeleteIndexOperation(usersByname));
                Assert.Null(await store.Admin.SendAsync(new GetIndexOperation(usersByname)));
            }
        }

        [Fact]
        public void CanGetTermsForIndex()
        {
            using (var store = GetDocumentStore())
            {
                using (var s = store.OpenSession())
                {
                    for (int i = 0; i < 15; i++)
                    {
                        s.Store(new User { Name = "user" + i.ToString("000") });
                    }
                    s.SaveChanges();
                }

                store.Admin.Send(new PutIndexOperation("test",
                    new IndexDefinition
                    {
                        Maps = { "from doc in docs.Users select new { doc.Name }" }
                    }));

                WaitForIndexing(store);

                var terms = store.Admin.Send(new GetTermsOperation("test", "Name", null, 10))
                    .OrderBy(x => x)
                    .ToArray();

                Assert.Equal(10, terms.Length);

                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal("user" + i.ToString("000"), terms.ElementAt(i));
                }
            }
        }

        [Fact]
        public void CanGetTermsForIndex_WithPaging()
        {
            using (var store = GetDocumentStore())
            {
                using (var s = store.OpenSession())
                {
                    for (int i = 0; i < 15; i++)
                    {
                        s.Store(new User { Name = "user" + i.ToString("000") });
                    }
                    s.SaveChanges();
                }

                store.Admin.Send(new PutIndexOperation("test",
                    new IndexDefinition
                    {
                        Maps = { "from doc in docs.Users select new { doc.Name }" }
                    }));

                WaitForIndexing(store);

                var terms = store.Admin.Send(new GetTermsOperation("test", "Name", "user009", 10))
                    .OrderBy(x => x)
                    .ToArray();

                Assert.Equal(5, terms.Count());

                for (int i = 10; i < 15; i++)
                {
                    Assert.Equal("user" + i.ToString("000"), terms.ElementAt(i - 10));
                }
            }
        }

        [Fact]
        public void CanQueryForMetadataAndIndexEntriesOnly()
        {
            using (var store = GetDocumentStore())
            {
                using (var s = store.OpenSession())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        s.Store(new User { Name = "user" + i });
                    }
                    s.SaveChanges();
                }

                store.Admin.Send(new PutIndexOperation("test",
                    new IndexDefinition
                    {
                        Maps = { "from doc in docs.Users select new { doc.Name }" },
                        Fields = new Dictionary<string, IndexFieldOptions>
                                                                 {
                                                                     { "Name", new IndexFieldOptions { Sort = SortOptions.String } }
                                                                 }
                    }));

                WaitForIndexing(store);

                using (var commands = store.Commands())
                {
                    var metadataOnly = commands.Query("test", new IndexQuery(store.Conventions), metadataOnly: true).Results;

                    foreach (BlittableJsonReaderObject item in metadataOnly)
                    {
                        Assert.Equal(1, item.Count);
                        BlittableJsonReaderObject _;
                        Assert.True(item.TryGet(Constants.Metadata.Key, out _));
                    }

                    var entriesOnly = commands.Query("test", new IndexQuery(store.Conventions)
                    {
                        SortedFields = new[] { new SortedField("Name") }
                    }, indexEntriesOnly: true).Results;

                    for (int i = 0; i < 5; i++)
                    {
                        var item = (BlittableJsonReaderObject)entriesOnly[i];

                        Assert.Equal(2, item.Count);

                        string name;
                        Assert.True(item.TryGet("Name", out name));
                        Assert.Equal("user" + i, name);

                        string id;
                        Assert.True(item.TryGet(Constants.Indexing.Fields.DocumentIdFieldName, out id));
                        Assert.Equal("users/" + (i + 1), id);
                    }
                }
            }
        }

        [Fact]
        public async Task CanGetIndexNames()
        {
            var index1 = new Users_ByName();
            var index2 = new Posts_ByTitleAndContent();
            using (var store = GetDocumentStore())
            {
                index1.Execute(store);
                index2.Execute(store);

                var indexes = await store.Admin.SendAsync(new GetIndexNamesOperation(0, 10));
                Assert.Equal(2, indexes.Length);
                Assert.Equal(index1.IndexName, indexes[1]);
                Assert.Equal(index2.IndexName, indexes[0]);
            }
        }

        [Fact]
        public async Task CanResetIndex()
        {
            var index = new Users_ByName();
            using (var store = GetDocumentStore())
            {
                index.Execute(store);
                using (var session = store.OpenSession())
                {
                    for (var i = 0; i < 20; i++)
                    {
                        session.Store(new User());
                    }

                    session.SaveChanges();
                }

                WaitForIndexing(store);

                var stats = await store.Admin.SendAsync(new GetStatisticsOperation());
                Assert.Equal(0, stats.StaleIndexes.Length);

                await store.Admin.SendAsync(new StopIndexingOperation());
                await store.Admin.SendAsync(new ResetIndexOperation(index.IndexName));

                stats = await store.Admin.SendAsync(new GetStatisticsOperation());
                Assert.Equal(1, stats.StaleIndexes.Length);
                Assert.Equal(index.IndexName, stats.StaleIndexes[0]);
            }
        }
    }
}