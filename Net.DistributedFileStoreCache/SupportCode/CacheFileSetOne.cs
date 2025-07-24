﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Net.DistributedFileStoreCache.SupportCode
{
    internal class CacheFileSetOne
    {
        private readonly string _key;
        private readonly string _value;
        private readonly DistributedCacheEntryOptions _timeoutOptions;

        public CacheFileSetOne(string key, string value, DistributedCacheEntryOptions timeoutOptions)
        {
            if (key == null) throw new ArgumentNullException(nameof(key), "The key cannot be null");
            if (value == null) throw new ArgumentNullException(nameof(value), "The value cannot be null");

            _key = key;
            _value = value;
            _timeoutOptions = timeoutOptions;
        }

        public void SetKeyValueHandler(ref CacheJsonContent currentJson)
        {
            currentJson.Cache[_key] = _value;
            ExpirationExtensions.SetupTimeoutIfOptions(ref currentJson, _key, _timeoutOptions);
        }
    }
}