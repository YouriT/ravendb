using FastTests;
using System.Linq;
using Raven.Client.Indexing;
using Raven.Client.Operations.Databases.Indexes;
using Raven.Client.PublicExtensions;
using Xunit;

namespace SlowTests.Bugs
{
    public class CanGetScores : RavenNewTestBase
    {
        private IndexFieldOptions filedOptions = new IndexFieldOptions {Indexing = FieldIndexing.Analyzed};

        [Fact]
        public void FromQuery()
        {
            using(var store = GetDocumentStore())
            {
                using(var s = store.OpenSession())
                {
                    s.Store(new User{ Name = "who is knocking on my doors"});
                    s.Store(new User { Name = "doors ltd" });
                    s.SaveChanges();
                }

                store.Admin.Send(new PutIndexOperation("test", new IndexDefinition
                {
                    Maps = { "from doc in docs select new { doc.Name}"},
                    Fields = { { "Name", filedOptions } }
                }));

                using (var s = store.OpenSession())
                {
                    var users = s.Query<User>("test")
                        .Customize(x => x.WaitForNonStaleResults())
                        .Where(x => x.Name == "doors")
                        .ToList();
                    Assert.Equal(2, users.Count);
                    foreach (var user in users)
                    {
                        var score = s.Advanced.GetMetadataFor(user)["@index-score"];
                        Assert.NotNull(score);
                    }
                }
            }
        }


        [Fact]
        public void FromQueryWithOrderByScoreThenName()
        {
            using (var store = GetDocumentStore ())
            {
                using (var s = store.OpenSession ())
                {
                    s.Store (new User { Name = "who is knocking on my doors" });
                    s.Store (new User { Name = "doors doors ltd" });
                    s.Store (new User { Name = "doors doors abc" });
                    s.SaveChanges ();
                }

                // Overloading the email property into a catchall freeform container to avoid rewriting the test entirely.

                store.Admin.Send(new PutIndexOperation("test", new IndexDefinition
                {
                    Maps = { "from doc in docs select new { Email = doc.Name, Name = doc.Name }" },
                    Fields = { { "Email", filedOptions } }
                }));

                using (var s = store.OpenSession ())
                {
                    var users = s.Query<User> ("test")
                        .Customize (x => x.WaitForNonStaleResults ())
                        .Where (x => x.Email == "doors")
                        .OrderByScore().ThenBy(x => x.Name)
                        .ToList ();

                    Assert.Equal (3, users.Count);

                    var sorted = (from u in users
                                    let score = s.Advanced.GetMetadataFor (u)["@index-score"]
                                    orderby score descending, u.Name
                                  select new { score, u.Name }).ToList ();

                    for (var i = 0; i < users.Count; i++)
                    {
                        Assert.Equal (sorted[i].Name, users[i].Name);
                        var score = s.Advanced.GetMetadataFor (users[i])["@index-score"];
                        Assert.NotNull (score);
                    }
                }
            }
        }

        [Fact]
        public void FromQueryWithOrderByScoreThenNameDescending()
        {
            using (var store = GetDocumentStore ())
            {
                using (var s = store.OpenSession ())
                {
                    s.Store (new User { Name = "who is knocking on my doors" });
                    s.Store (new User { Name = "doors doors ltd" });
                    s.Store (new User { Name = "doors doors abc" });
                    s.SaveChanges ();
                }

                // Overloading the email property into a catchall freeform container to avoid rewriting the test entirely.

                store.Admin.Send(new PutIndexOperation("test", new IndexDefinition
                {
                    Maps = { "from doc in docs select new { Email = doc.Name, Name = doc.Name }" },
                    Fields = { { "Email", filedOptions } }
                }));

                using (var s = store.OpenSession ())
                {
                    var users = s.Query<User> ("test")
                        .Customize (x => x.WaitForNonStaleResults ())
                        .Where (x => x.Email == "doors")
                        .OrderByScore ().ThenByDescending(x => x.Name)
                        .ToList ();

                    Assert.Equal (3, users.Count);

                    var sorted = (from u in users
                                  let score = s.Advanced.GetMetadataFor (u)["@index-score"]
                                  orderby score descending, u.Name descending
                                  select new { score, u.Name }).ToList ();

                    for (var i = 0; i < users.Count; i++)
                    {
                        Assert.Equal (sorted[i].Name, users[i].Name);
                        var score = s.Advanced.GetMetadataFor (users[i])["@index-score"];
                        Assert.NotNull (score);
                    }
                }
            }
        }

        private class User
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string PartnerId { get; set; }
            public string Email { get; set; }
            public string[] Tags { get; set; }
            public int Age { get; set; }
            public bool Active { get; set; }
        }

    }
}
