using System.Collections.Generic;

using Xunit;
using System.Linq;
using FastTests;
using Raven.Client.Indexing;
using Raven.Client.Operations.Databases.Indexes;

namespace SlowTests.Issues
{
    public class RavenDB_10 : RavenNewTestBase
    {
        private class Item
        {
            public string Text { get; set; }
            public int Age { get; set; }
        }

        [Fact]
        public void ShouldSortCorrectly()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Item { Age = 10 });
                    session.Store(new Item { Age = 3 });

                    session.SaveChanges();
                }
                using (var session = store.OpenSession())
                {
                    var items = session.Query<Item>()
                        .Customize(x => x.WaitForNonStaleResults())
                        .OrderBy(x => x.Age)
                        .ToList();


                    Assert.Equal(3, items[0].Age);
                    Assert.Equal(10, items[1].Age);
                }
            }
        }

        [Fact]
        public void ShouldSearchCorrectly()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Item { Text = "Seek's" });
                    session.Store(new Item { Text = "Sit" });

                    session.SaveChanges();
                }
                var opt = new IndexFieldOptions { Analyzer = typeof(Lucene.Net.Analysis.Standard.StandardAnalyzer).AssemblyQualifiedName, Indexing = FieldIndexing.Analyzed };

                store.Admin.Send(new PutIndexOperation("test", new IndexDefinition
                {
                    Maps = new HashSet<string> { "from doc in docs select new { doc.Text }" },
                    Fields = { { "Text", opt } },
                }));

                using (var session = store.OpenSession())
                {
                    Assert.NotEmpty(session.Query<Item>("test")
                                        .Customize(x => x.WaitForNonStaleResults())
                                        .Where(x => x.Text == "Seek")
                                        .ToList());

                    Assert.NotEmpty(session.Query<Item>("test")
                                        .Customize(x => x.WaitForNonStaleResults())
                                        .Where(x => x.Text == "Sit's")
                                        .ToList());
                }
            }
        }
    }
}
