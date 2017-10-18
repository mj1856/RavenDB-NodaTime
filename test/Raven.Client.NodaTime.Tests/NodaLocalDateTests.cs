﻿using System.Diagnostics;
using System.Linq;
using NodaTime;
using NodaTime.Text;
using Raven.Client.Indexes;
using Raven.Imports.Newtonsoft.Json;
using Raven.Tests.Helpers;
using Xunit;

namespace Raven.Client.NodaTime.Tests
{
    public class NodaLocalDateTests : RavenTestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Can_Use_NodaTime_LocalDate_In_Document_Today(bool useRelaxedConverters)
        {
            Can_Use_NodaTime_LocalDate_In_Document(NodaUtil.LocalDate.Today, useRelaxedConverters);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Can_Use_NodaTime_LocalDate_In_Document_IsoMin(bool useRelaxedConverters)
        {
            Can_Use_NodaTime_LocalDate_In_Document(NodaUtil.LocalDate.MinIsoValue, useRelaxedConverters);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Can_Use_NodaTime_LocalDate_In_Document_IsoMax(bool useRelaxedConverters)
        {
            Can_Use_NodaTime_LocalDate_In_Document(NodaUtil.LocalDate.MaxIsoValue, useRelaxedConverters);
        }

        private void Can_Use_NodaTime_LocalDate_In_Document(LocalDate ld, bool useRelaxedConverters)
        {
            using (var documentStore = NewDocumentStore())
            {
                if (useRelaxedConverters)
                {
                    using (var session = documentStore.OpenSession())
                    {
                        session.Store(new Foo {Id = "foos/1", LocalDate = ld});

                        // save localdate as nodatime localdate
                        session.SaveChanges();
                    }
                }

                documentStore.ConfigureForNodaTime(useRelaxedConverters);

                using (var session = documentStore.OpenSession())
                {
                    if (useRelaxedConverters)
                    {
                        var foo = session.Load<Foo>("foos/1");

                        // we can read localdate saved as nodatime localdate
                        Assert.Equal(ld, foo.LocalDate);

                        session.Store(foo);
                    }
                    else
                    {
                        session.Store(new Foo { Id = "foos/1", LocalDate = ld });
                    }

                    // save duration as iso pattern
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var foo = session.Load<Foo>("foos/1");

                    // we can read localdate saved as iso pattern
                    Assert.Equal(ld, foo.LocalDate);
                }

                var json = documentStore.DatabaseCommands.Get("foos/1").DataAsJson;
                Debug.WriteLine(json.ToString(Formatting.Indented));
                var expected = ld.ToString(LocalDatePattern.Iso.PatternText, null);
                Assert.Equal(expected, json.Value<string>("LocalDate"));
            }
        }

        [Fact]
        public void Can_Use_NodaTime_LocalDate_In_Dynamic_Index_Today()
        {
            Can_Use_NodaTime_LocalDate_In_Dynamic_Index1(NodaUtil.LocalDate.Today);
        }

        [Fact]
        public void Can_Use_NodaTime_LocalDate_In_Dynamic_Index_IsoMin()
        {
            Can_Use_NodaTime_LocalDate_In_Dynamic_Index1(NodaUtil.LocalDate.MinIsoValue);
        }

        [Fact]
        public void Can_Use_NodaTime_LocalDate_In_Dynamic_Index_IsoMax()
        {
            Can_Use_NodaTime_LocalDate_In_Dynamic_Index2(NodaUtil.LocalDate.MaxIsoValue);
        }

        private void Can_Use_NodaTime_LocalDate_In_Dynamic_Index1(LocalDate ld)
        {
            using (var documentStore = NewDocumentStore())
            {
                documentStore.ConfigureForNodaTime();

                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Foo { Id = "foos/1", LocalDate = ld });
                    session.Store(new Foo { Id = "foos/2", LocalDate = ld + Period.FromDays(1) });
                    session.Store(new Foo { Id = "foos/3", LocalDate = ld + Period.FromDays(2) });
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var q1 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate == ld);
                    var results1 = q1.ToList();
                    Assert.Equal(1, results1.Count);

                    var q2 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate > ld)
                                    .OrderByDescending(x => x.LocalDate);
                    var results2 = q2.ToList();
                    Assert.Equal(2, results2.Count);
                    Assert.True(results2[0].LocalDate > results2[1].LocalDate);

                    var q3 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate >= ld)
                                    .OrderByDescending(x => x.LocalDate);
                    var results3 = q3.ToList();
                    Assert.Equal(3, results3.Count);
                    Assert.True(results3[0].LocalDate > results3[1].LocalDate);
                    Assert.True(results3[1].LocalDate > results3[2].LocalDate);
                }
            }
        }

        private void Can_Use_NodaTime_LocalDate_In_Dynamic_Index2(LocalDate ld)
        {
            using (var documentStore = NewDocumentStore())
            {
                documentStore.ConfigureForNodaTime();

                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Foo { Id = "foos/1", LocalDate = ld });
                    session.Store(new Foo { Id = "foos/2", LocalDate = ld - Period.FromDays(1) });
                    session.Store(new Foo { Id = "foos/3", LocalDate = ld - Period.FromDays(2) });
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var q1 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate == ld);
                    var results1 = q1.ToList();
                    Assert.Equal(1, results1.Count);

                    var q2 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate < ld)
                                    .OrderBy(x => x.LocalDate);
                    var results2 = q2.ToList();
                    Assert.Equal(2, results2.Count);
                    Assert.True(results2[0].LocalDate < results2[1].LocalDate);

                    var q3 = session.Query<Foo>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate <= ld)
                                    .OrderBy(x => x.LocalDate);
                    var results3 = q3.ToList();
                    Assert.Equal(3, results3.Count);
                    Assert.True(results3[0].LocalDate < results3[1].LocalDate);
                    Assert.True(results3[1].LocalDate < results3[2].LocalDate);
                }
            }
        }

        [Fact]
        public void Can_Use_NodaTime_LocalDate_In_Static_Index_Today()
        {
            Can_Use_NodaTime_LocalDate_In_Static_Index1(NodaUtil.LocalDate.Today);
        }

        [Fact]
        public void Can_Use_NodaTime_LocalDate_In_Static_Index_IsoMin()
        {
            Can_Use_NodaTime_LocalDate_In_Static_Index1(NodaUtil.LocalDate.MinIsoValue);
        }

        [Fact]
        public void Can_Use_NodaTime_LocalDate_In_Static_Index_IsoMax()
        {
            Can_Use_NodaTime_LocalDate_In_Static_Index2(NodaUtil.LocalDate.MaxIsoValue);
        }

        private void Can_Use_NodaTime_LocalDate_In_Static_Index1(LocalDate ld)
        {
            using (var documentStore = NewDocumentStore())
            {
                documentStore.ConfigureForNodaTime();
                documentStore.ExecuteIndex(new TestIndex());

                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Foo { Id = "foos/1", LocalDate = ld });
                    session.Store(new Foo { Id = "foos/2", LocalDate = ld + Period.FromDays(1) });
                    session.Store(new Foo { Id = "foos/3", LocalDate = ld + Period.FromDays(2) });
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var q1 = session.Query<Foo, TestIndex>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate == ld);
                    var results1 = q1.ToList();
                    Assert.Equal(1, results1.Count);

                    var q2 = session.Query<Foo, TestIndex>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate > ld)
                                    .OrderByDescending(x => x.LocalDate);
                    var results2 = q2.ToList();
                    Assert.Equal(2, results2.Count);
                    Assert.True(results2[0].LocalDate > results2[1].LocalDate);

                    var q3 = session.Query<Foo, TestIndex>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate >= ld)
                                    .OrderByDescending(x => x.LocalDate);
                    var results3 = q3.ToList();
                    Assert.Equal(3, results3.Count);
                    Assert.True(results3[0].LocalDate > results3[1].LocalDate);
                    Assert.True(results3[1].LocalDate > results3[2].LocalDate);
                }
            }
        }

        private void Can_Use_NodaTime_LocalDate_In_Static_Index2(LocalDate ld)
        {
            using (var documentStore = NewDocumentStore())
            {
                documentStore.ConfigureForNodaTime();
                documentStore.ExecuteIndex(new TestIndex());

                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Foo { Id = "foos/1", LocalDate = ld });
                    session.Store(new Foo { Id = "foos/2", LocalDate = ld - Period.FromDays(1) });
                    session.Store(new Foo { Id = "foos/3", LocalDate = ld - Period.FromDays(2) });
                    session.SaveChanges();
                }

                using (var session = documentStore.OpenSession())
                {
                    var q1 = session.Query<Foo, TestIndex>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate == ld);
                    var results1 = q1.ToList();
                    Assert.Equal(1, results1.Count);

                    var q2 = session.Query<Foo, TestIndex>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate < ld)
                                    .OrderBy(x => x.LocalDate);
                    var results2 = q2.ToList();
                    Assert.Equal(2, results2.Count);
                    Assert.True(results2[0].LocalDate < results2[1].LocalDate);

                    var q3 = session.Query<Foo, TestIndex>().Customize(x => x.WaitForNonStaleResults())
                                    .Where(x => x.LocalDate <= ld)
                                    .OrderBy(x => x.LocalDate);
                    var results3 = q3.ToList();
                    Assert.Equal(3, results3.Count);
                    Assert.True(results3[0].LocalDate < results3[1].LocalDate);
                    Assert.True(results3[1].LocalDate < results3[2].LocalDate);
                }
            }
        }

        public class Foo
        {
            public string Id { get; set; }
            public LocalDate LocalDate { get; set; }
        }

        public class TestIndex : AbstractIndexCreationTask<Foo>
        {
            public TestIndex()
            {
                Map = foos => from foo in foos
                              select new
                              {
                                  foo.LocalDate
                              };
            }
        }
    }
}
