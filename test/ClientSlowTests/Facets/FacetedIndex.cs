//-----------------------------------------------------------------------
// <copyright file="FacetedIndex.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Xunit;
using System.Linq;
using Raven.Client.Commands;
using Raven.Client.Data;
using Raven.Client.Document;
using Raven.Client.PublicExtensions;

namespace NewClientTests.NewClient
{
    public class FacetedIndex : FacetTestBase
    {
        private readonly IList<Camera> _data;
        private readonly List<Facet> _originalFacets;
        private readonly List<Facet> _stronglyTypedFacets;
        private const int NumCameras = 1000;

        public FacetedIndex()
        {
            _data = GetCameras(NumCameras);

            _originalFacets = new List<Facet>
                        {
                            new Facet {Name = "Manufacturer"},
                            //default is term query		                         
                            //In Lucene [ is inclusive, { is exclusive
                            new Facet
                                {
                                    Name = "Cost_Range",
                                    Mode = FacetMode.Ranges,
                                    Ranges =
                                        {
                                            "[NULL TO Dx200]",
                                            "[Dx200 TO Dx400]",
                                            "[Dx400 TO Dx600]",
                                            "[Dx600 TO Dx800]",
                                            "[Dx800 TO NULL]",
                                        }
                                },
                            new Facet
                                {
                                    Name = "Megapixels_Range",
                                    Mode = FacetMode.Ranges,
                                    Ranges =
                                        {
                                            "[NULL TO Dx3]",
                                            "[Dx3 TO Dx7]",
                                            "[Dx7 TO Dx10]",
                                            "[Dx10 TO NULL]",
                                        }
                                }
                        };

            _stronglyTypedFacets = GetFacets();
        }

        [Fact]
        public void CanPerformFacetedSearch()
        {
            using (var store = GetDocumentStore())
            {
                ExecuteTest(store, _originalFacets);
            }
        }

        [Fact]
        public void CanPerformFacetedSearch_Asynchronously()
        {
            using (var store = GetDocumentStore())
            {
                ExecuteTestAsynchronously(store, _originalFacets);
            }
        }

        [Fact]
        public void CanPerformFacetedSearch_WithStronglyTypedAPI()
        {
            using (var store = GetDocumentStore())
            {
                ExecuteTest(store, _stronglyTypedFacets);
            }
        }

        [Fact]
        public void CanPerformFacetedSearch_Lazy()
        {
            using (var store = GetDocumentStore())
            {
                Setup(store, _originalFacets);

                using (var s = store.OpenSession())
                {
                    var expressions = new Expression<Func<Camera, bool>>[]
                    {
                        x => x.Cost >= 100 && x.Cost <= 300,
                        x => x.DateOfListing > new DateTime(2000, 1, 1),
                        x => x.Megapixels > 5.0m && x.Cost < 500
                    };


                    foreach (var exp in expressions)
                    {
                        var oldRequests = s.Advanced.NumberOfRequests;

                        var facetResults = s.Query<Camera>("CameraCost")
                            .Customize(x => x.WaitForNonStaleResults())
                            .Where(exp)
                            .ToFacetsLazy("facets/CameraFacets");

                        Assert.Equal(oldRequests, s.Advanced.NumberOfRequests);

                        var filteredData = _data.Where(exp.Compile()).ToList();
                        CheckFacetResultsMatchInMemoryData(facetResults.Value, filteredData);

                        Assert.Equal(oldRequests + 1, s.Advanced.NumberOfRequests);
                    }
                }
            }
        }

        [Fact]
        public void CanPerformFacetedSearch_Lazy_can_work_with_others()
        {
            using (var store = GetDocumentStore())
            {
                Setup(store, _originalFacets);

                using (var s = store.OpenSession())
                {
                    var expressions = new Expression<Func<Camera, bool>>[]
                    {
                        x => x.Cost >= 100 && x.Cost <= 300,
                        x => x.DateOfListing > new DateTime(2000, 1, 1),
                        x => x.Megapixels > 5.0m && x.Cost < 500
                    };

                    foreach (var exp in expressions)
                    {
                        var oldRequests = s.Advanced.NumberOfRequests;
                        var load = s.Advanced.Lazily.Load<Camera>($"cameras/{oldRequests}");
                        var facetResults = s.Query<Camera>("CameraCost")
                            .Customize(x => x.WaitForNonStaleResults())
                            .Where(exp)
                            .ToFacetsLazy("facets/CameraFacets");

                        Assert.Equal(oldRequests, s.Advanced.NumberOfRequests);

                        var filteredData = _data.Where(exp.Compile()).ToList();
                        CheckFacetResultsMatchInMemoryData(facetResults.Value, filteredData);
                        var forceLoading = load.Value;
                        Assert.Equal(oldRequests + 1, s.Advanced.NumberOfRequests);
                    }
                }
            }
        }

        private void ExecuteTest(DocumentStore store, List<Facet> facetsToUse)
        {
            Setup(store, facetsToUse);

            using (var s = store.OpenSession())
            {
                var expressions = new Expression<Func<Camera, bool>>[]
                {
                    x => x.Cost >= 100 && x.Cost <= 300,
                    x => x.DateOfListing > new DateTime(2000, 1, 1),
                    x => x.Megapixels > 5.0m && x.Cost < 500,
                    x => x.Manufacturer == "abc&edf"
                };

                foreach (var exp in expressions)
                {
                    var facetQueryTimer = Stopwatch.StartNew();
                    var facetResults = s.Query<Camera>("CameraCost")
                        .Customize(x => x.WaitForNonStaleResults())
                        .Where(exp)
                        .ToFacets("facets/CameraFacets");
                    facetQueryTimer.Stop();

                    var filteredData = _data.Where(exp.Compile()).ToList();
                    CheckFacetResultsMatchInMemoryData(facetResults, filteredData);
                }
            }
        }

        private void ExecuteTestAsynchronously(DocumentStore store, List<Facet> facetsToUse)
        {
            Setup(store, facetsToUse);

            using (var s = store.OpenAsyncSession())
            {
                var expressions = new Expression<Func<Camera, bool>>[]
                {
                    x => x.Cost >= 100 && x.Cost <= 300,
                    x => x.DateOfListing > new DateTime(2000, 1, 1),
                    x => x.Megapixels > 5.0m && x.Cost < 500,
                    x => x.Manufacturer == "abc&edf"
                };

                foreach (var exp in expressions)
                {
                    var facetQueryTimer = Stopwatch.StartNew();
                    var task = s.Query<Camera>("CameraCost")
                        .Customize(x => x.WaitForNonStaleResults())
                        .Where(exp)
                        .ToFacetsAsync("facets/CameraFacets");
                    task.Wait();
                    facetQueryTimer.Stop();
                    var facetResults = task.Result;
                    var filteredData = _data.Where(exp.Compile()).ToList();
                    CheckFacetResultsMatchInMemoryData(facetResults, filteredData);
                }
            }
        }

        private void Setup(DocumentStore store, List<Facet> facetsToUse)
        {
            using (var s = store.OpenSession())
            {
                var facetSetupDoc = new FacetSetup { Id = "facets/CameraFacets", Facets = facetsToUse };
                s.Store(facetSetupDoc);
                s.SaveChanges();
            }

            CreateCameraCostIndex(store);

            InsertCameraData(store, _data);
        }

        private void CheckFacetResultsMatchInMemoryData(
                     FacetedQueryResult facetResults,
                     List<Camera> filteredData)
        {
            //Make sure we get all range values
            Assert.Equal(filteredData.GroupBy(x => x.Manufacturer).Count(),
                        facetResults.Results["Manufacturer"].Values.Count());

            foreach (var facet in facetResults.Results["Manufacturer"].Values)
            {
                var inMemoryCount = filteredData.Count(x => x.Manufacturer.ToLower() == facet.Range);
                Assert.Equal(inMemoryCount, facet.Hits);
            }

            //Go through the expected (in-memory) results and check that there is a corresponding facet result
            //Not the prettiest of code, but it works!!!
            var costFacets = facetResults.Results["Cost_Range"].Values;
            CheckFacetCount(filteredData.Count(x => x.Cost <= 200.0m),
                            costFacets.FirstOrDefault(x => x.Range == "[NULL TO Dx200]"));
            CheckFacetCount(filteredData.Count(x => x.Cost >= 200.0m && x.Cost <= 400),
                            costFacets.FirstOrDefault(x => x.Range == "[Dx200 TO Dx400]"));
            CheckFacetCount(filteredData.Count(x => x.Cost >= 400.0m && x.Cost <= 600.0m),
                            costFacets.FirstOrDefault(x => x.Range == "[Dx400 TO Dx600]"));
            CheckFacetCount(filteredData.Count(x => x.Cost >= 600.0m && x.Cost <= 800.0m),
                            costFacets.FirstOrDefault(x => x.Range == "[Dx600 TO Dx800]"));
            CheckFacetCount(filteredData.Count(x => x.Cost >= 800.0m),
                            costFacets.FirstOrDefault(x => x.Range == "[Dx800 TO NULL]"));

            //Test the Megapixels_Range facets using the same method
            var megapixelsFacets = facetResults.Results["Megapixels_Range"].Values;
            CheckFacetCount(filteredData.Where(x => x.Megapixels <= 3.0m).Count(),
                            megapixelsFacets.FirstOrDefault(x => x.Range == "[NULL TO Dx3]"));
            CheckFacetCount(filteredData.Where(x => x.Megapixels >= 3.0m && x.Megapixels <= 7.0m).Count(),
                            megapixelsFacets.FirstOrDefault(x => x.Range == "[Dx3 TO Dx7]"));
            CheckFacetCount(filteredData.Where(x => x.Megapixels >= 7.0m && x.Megapixels <= 10.0m).Count(),
                            megapixelsFacets.FirstOrDefault(x => x.Range == "[Dx7 TO Dx10]"));
            CheckFacetCount(filteredData.Where(x => x.Megapixels >= 10.0m).Count(),
                            megapixelsFacets.FirstOrDefault(x => x.Range == "[Dx10 TO NULL]"));
        }

        private void CheckFacetCount(int expectedCount, FacetValue facets)
        {
            if (expectedCount > 0)
            {
                Assert.NotNull(facets);
                Assert.Equal(expectedCount, facets.Hits);
            }
        }
    }
}
