using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.ServiceModel;

namespace CGN.Paralegal.Infrastructure.Common
{
    public static class RequestCallContext
    {
        /// <summary>
        /// To store data in current request context.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        public static void Put(string key, object obj)
        {
            if (OperationContext.Current != null && OperationContext.Current.Extensions != null)
            {
                ContextBag contextBag = OperationContext.Current.Extensions.Find<ContextBag>();
                if (contextBag == null)
                {
                    contextBag = new ContextBag();
                    OperationContext.Current.Extensions.Add(contextBag);

                }
                if (contextBag.State != null && !contextBag.State.ContainsKey(key))
                {
                    contextBag.State.Add(new KeyValuePair<string, object>(key, obj));
                }
                
            }
            else if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Items[key] != null)
                {
                    HttpContext.Current.Items[key] = obj;
                }
                else
                {
                    HttpContext.Current.Items.Add(key, obj);
                }
            }
        }

        /// <summary>
        /// To get the data from current request context.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(string key)
        {
            object returnObj = null;
            if (OperationContext.Current != null && OperationContext.Current.Extensions != null)
            {
                ContextBag contextBag = OperationContext.Current.Extensions.Find<ContextBag>();
                if (contextBag != null)
                {
                    returnObj = (T)contextBag.State[key];

                }
            }
            else if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Items[key] != null)
                {
                    returnObj = (T)HttpContext.Current.Items[key];
                }
            }
            return (T)returnObj;
        }

        /// <summary>
        /// To check the data exists in current request context.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsExist(string key)
        {
            bool exists = false;
            if (OperationContext.Current != null && OperationContext.Current.Extensions != null)
            {
                ContextBag contextBag = OperationContext.Current.Extensions.Find<ContextBag>();
                if (contextBag != null && contextBag.State.ContainsKey(key))
                {
                    exists = true;
                }
            }
            else if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Items[key] != null)
                {
                    exists = true;
                }
            }
            return exists;
        }
    }
}
