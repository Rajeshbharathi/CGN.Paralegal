//---------------------------------------------------------------------------------------------------
// <copyright file="GlobalCache.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi</author>
//      <description>
//          Holds the global cache object
//      </description>
//      <change log>
//          <date value=""></date>
//      </change log>
// </header>
//---------------------------------------------------------------------------------------------------

using System.Runtime.Caching;
using CGN.Paralegal.TraceServices;

namespace CGN.Paralegal.Infrastructure.Caching
{
    /// <summary>
    ///     Derived from CacheManagerBase which holds the global cache object.
    /// </summary>
    public class GlobalCache


    {
        /// <summary>
        ///     The refresh interval
        /// </summary>
        private const int RefreshInterval = 60;

        //ToDo:EL6 - Should it be in configuration ?


        /// <summary>
        ///     The _memory cache
        /// </summary>
        private readonly MemoryCache _memoryCache;

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the GlobalCache class.
        /// </summary>
        public GlobalCache()
        {
            _memoryCache = MemoryCache.Default;
        }

        #endregion

        #region member functions

        /// <summary>
        ///     Adds the cache object to cache manager object
        /// </summary>
        /// <param name="key">Key to cache manager collection</param>
        /// <param name="value">value to cache manager collection</param>
        public virtual void Add(string key, object value)
        {
            key.ShouldNotBeEmpty();
            _memoryCache.Set(key, value, null);
        }

        /// <summary>
        ///     Removes the cache object from cache manager object
        /// </summary>
        /// <param name="key">Key to the cache manager collection</param>
        public virtual void Remove(string key)
        {
            key.ShouldNotBeEmpty();
            _memoryCache.Remove(key);
        }

        /// <summary>
        ///     Flushes the cache manager object
        /// </summary>
        public virtual void Flush()
        {
            _memoryCache.Dispose();
        }

        /// <summary>
        ///     Gets the data from cache manager collection.
        /// </summary>
        /// <param name="key">Key to the cache manager collection</param>
        /// <returns>object type</returns>
        public virtual object GetData(string key)
        {
            key.ShouldNotBeEmpty();
            return _memoryCache.Get(key);
        }

        /// <summary>
        ///     Checks the cache manager collection contains the cache object
        /// </summary>
        /// <param name="key">key to the cache manager collection</param>
        /// <returns>true / false</returns>
        public virtual bool Contains(string key)
        {
            key.ShouldNotBeEmpty();
            return _memoryCache.Contains(key);
        }

        #endregion
    }
}