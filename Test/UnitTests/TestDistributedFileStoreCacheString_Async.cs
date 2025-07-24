﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;
using Net.DistributedFileStoreCache.SupportCode;
using Test.TestHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests
{
    // see https://stackoverflow.com/questions/1408175/execute-unit-tests-serially-rather-than-in-parallel
    [Collection("Sequential")]
    public class TestDistributedFileStoreCacheString_Async 
    {
        private readonly IDistributedFileStoreCacheString _distributedCache;
        private readonly DistributedFileStoreCacheOptions _options;
        private readonly ITestOutputHelper _output;

        public TestDistributedFileStoreCacheString_Async(ITestOutputHelper output)
        {
            _output = output;

            var services = new ServiceCollection();
            _options = services.AddDistributedFileStoreCache(options =>
            {
                options.WhichVersion = FileStoreCacheVersions.String;
                options.PathToCacheFileDirectory = TestData.GetTestDataDir();
                options.SecondPartOfCacheFileName = GetType().Name;
                options.TurnOffStaticFilePathCheck = true;
            });
            var serviceProvider = services.BuildServiceProvider();

            _distributedCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheString>();
        }

        [Fact]
        public async Task DistributedFileStoreCacheSetAsync()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            await _distributedCache.SetAsync("test", "hello async");

            //VERIFY
            var value = await _distributedCache.GetAsync("test");
            value.ShouldEqual("hello async");

            var allValues = await _distributedCache.GetAllKeyValuesAsync();
            allValues.Count.ShouldEqual(1);
            allValues["test"].ShouldEqual("hello async");

            _options.DisplayCacheFile(_output);
        }

        [Fact]
        public async Task DistributedFileStoreCacheSet_AbsoluteExpirationStillValid()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            await _distributedCache.SetAsync("test-timeout1Sec", "time1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1) });

            //VERIFY
            (await _distributedCache.GetAsync("test-timeout1Sec")).ShouldEqual("time1");
            StaticCachePart.CacheContent.TimeOuts["test-timeout1Sec"].ShouldNotBeNull();

            _options.DisplayCacheFile(_output);
        }

        [Fact]
        public async Task DistributedFileStoreCacheSet_AbsoluteExpirationExpired()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            await _distributedCache.SetAsync("test-timeoutExpired", "time1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(1) });

            //VERIFY
            (await _distributedCache.GetAsync("test-timeoutExpired")).ShouldBeNull();
            StaticCachePart.CacheContent.TimeOuts.ContainsKey("test-timeout1Sec").ShouldBeFalse();

            _options.DisplayCacheFile(_output);
        }

        [Fact]
        public async Task DistributedFileStoreCacheSet_SlidingExpiration()
        {
            //SETUP

            //ATTEMPT
            var ex = await Assert.ThrowsAsync<NotImplementedException>( async () => await _distributedCache.SetAsync("test-bad", "time1",
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromTicks(1) }));

            //VERIFY
            ex.Message.ShouldEqual("This library doesn't support sliding expirations for performance reasons.");
        }

        [Fact]
        public async Task DistributedFileStoreCacheSetNullBad()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            try
            {
                await _distributedCache.SetAsync("test", null);
            }
            catch (ArgumentNullException)
            {
                return;
            }

            //VERIFY
            Assert.True(false, "should have throw exception");
        }

        [Fact]
        public async Task DistributedFileStoreCacheWithSetChangeAsync()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            await _distributedCache.SetAsync("test", "first");
            _options.DisplayCacheFile(_output);
            await _distributedCache.SetAsync("test", "second");
            _options.DisplayCacheFile(_output);

            //VERIFY
            var value = await _distributedCache.GetAsync("test");
            value.ShouldEqual("second");
            var allValues = await _distributedCache.GetAllKeyValuesAsync();
            allValues.Count.ShouldEqual(1);
        }

        [Fact]
        public async Task DistributedFileStoreCacheRemoveAsync()
        {
            //SETUP
            _distributedCache.ClearAll();
            await _distributedCache.SetAsync("YYY", "another to go");
            await _distributedCache.SetAsync("Still there", "keep this");

            //ATTEMPT
            await _distributedCache.RemoveAsync("YYY");

            //VERIFY
            (await _distributedCache.GetAsync("YYY")).ShouldBeNull();
            (await _distributedCache.GetAllKeyValuesAsync()).Count.ShouldEqual(1);

            _options.DisplayCacheFile(_output);
        }

        [Fact]
        public async Task DistributedFileStoreCacheSetTwice()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            await _distributedCache.SetManyAsync(new List<KeyValuePair<string, string>>
            {
                new ("test1", "first"),
                new ("test2", "second")
            });

            //VERIFY
            var allValues = await _distributedCache.GetAllKeyValuesAsync();
            allValues.Count.ShouldEqual(2);
            allValues["test1"].ShouldEqual("first");
            allValues["test2"].ShouldEqual("second");

            _options.DisplayCacheFile(_output);
        }

        [Fact]
        public async Task DistributedFileStoreCacheSetMany()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            await _distributedCache.SetManyAsync(new List<KeyValuePair<string, string>>
            {
                new ("test1", "first"),
                new ("test2", "second")
            });

            //VERIFY
            var allValues = await _distributedCache.GetAllKeyValuesAsync();
            allValues.Count.ShouldEqual(2);
            allValues["test1"].ShouldEqual("first");
            allValues["test2"].ShouldEqual("second");

            _options.DisplayCacheFile(_output);
        }

        [Fact]
        public async Task DistributedFileStoreCacheSetMany_AbsoluteExpirationRelativeToNow()
        {
            //SETUP
            _distributedCache.ClearAll();

            //ATTEMPT
            await _distributedCache.SetManyAsync(new List<KeyValuePair<string, string>>
            {
                new ("Timeout1", "first"),
                new ("Timeout2", "second")
            }, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(1) });
            await _distributedCache.SetAsync("NotTimedOut", "I'm still here");

            //VERIFY
            var allValues = await _distributedCache.GetAllKeyValuesAsync();
            allValues.Count.ShouldEqual(1);
            allValues["NotTimedOut"].ShouldEqual("I'm still here");
            StaticCachePart.CacheContent.TimeOuts.ContainsKey("Timeout1").ShouldBeFalse();

            _options.DisplayCacheFile(_output);
        }

        [Fact]
        public async Task DistributedFileStoreCacheHeavyUsage()
        {
            //SETUP
        

            //ATTEMPT
            for (int i = 0; i < 10; i++)
            {
                _distributedCache.ClearAll();
                await _distributedCache.SetAsync($"test{i}", i.ToString());
                _distributedCache.Get($"test{i}").ShouldEqual(i.ToString());
            }


            //VERIFY
            var allValues = await _distributedCache.GetAllKeyValuesAsync();
            allValues.Count.ShouldEqual(1);

            _options.DisplayCacheFile(_output);
        }
    }
}