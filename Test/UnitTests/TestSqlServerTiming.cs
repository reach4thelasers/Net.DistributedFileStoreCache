﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.EntityFrameworkCore;
using TestSupport.Attributes;
using TestSupport.EfHelpers;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests
{
    public class TestSqlServerTiming
    {
        private readonly ITestOutputHelper _output;

        public TestSqlServerTiming(ITestOutputHelper output)
        {
            _output = output;
        }

        public class MyCache
        {
            [Key]
            [MaxLength(100)]
            public string Name { get; set; }
            public string Value { get; set; }
        }



        public class TestDbContext : DbContext
        {
            public DbSet<MyCache> Cache { get; set; }

            public TestDbContext(DbContextOptions<TestDbContext> options)
                : base(options) { }
        }



        //I build this to see how quick a sql server cache could be
        //Remember: a Set does an create or update, while the SQL only does create part
        //That means that the SQL performance is better than a SQL cache library 
        [RunnableInDebugOnly]
        public void TestSqlServerRaw()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<TestDbContext>();
            var context = new TestDbContext(options);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            const int NumTest = 100;
            //warmup
            for (int i = 0; i < 10; i++)
            {
                var insert = String.Format("INSERT INTO Cache (Name, Value) VALUES ('{0}', '{1}')", $"Key1{i:D4}",
                    DateTime.Now.Ticks.ToString());
                context.Database.ExecuteSqlRaw(insert);
            }

            //ATTEMPT
            using (new TimeThings(_output, "sql write", NumTest))
            {
                for (int i = 0; i < NumTest; i++)
                {
                    var sql = String.Format("INSERT INTO Cache (Name, Value) VALUES ('{0}', '{1}')", $"Key2{i:D4}",
                        DateTime.Now.Ticks.ToString());

                    context.Database.GetDbConnection().QuerySingleOrDefault<string>(sql);
                }
            }

            string read = null;
            using (new TimeThings(_output, "sql read", NumTest))
            {
                for (int i = 0; i < NumTest; i++)
                {
                    var sql = $"SELECT [c].[Value] FROM [Cache] AS [c] WHERE [c].[Name] = 'Key2{i:D4}'";
                    read = context.Database.GetDbConnection().QuerySingleOrDefault<string>(sql);
                }
            }
            read.ShouldNotBeNull();

            //VERIFY
        }
    }
}