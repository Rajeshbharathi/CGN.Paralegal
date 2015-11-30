using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CGN.Paralegal.Infrastructure.ServerManagement;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;

namespace CGN.Paralegal.Infrastructure.EVContainer
{
    /// <summary>
    ///     Wrapper class to the UnityContainers which helps to inject dependency object.
    /// </summary>
    public static class EVUnityContainer

    {
        /// <summary>
        ///     Private variable of UnityContainer object
        /// </summary>
        private static IUnityContainer _container;

        /*RI: Do not do anything extensive in static constructors, like reading config files or creating extensive object hierarchies
        //  execute this code the first read of Container property */

        /// <summary>
        ///     Gets the UnityContainer object
        /// </summary>
        private static IUnityContainer Container
        {
            get
            {
                if (_container != null) return _container;
                var unityContainer = new UnityContainer();
                var section = (UnityConfigurationSection) ConfigurationManager.GetSection("unity");
                section.Configure(unityContainer);
                _container = unityContainer;
                _container.RegisterType<IPingWrapper, PingWrapper>();
                return _container;
            }
        }

        /// <summary>
        ///     Resolves or Creates a new instance of T
        /// </summary>
        /// <typeparam name="T">Generic type T</typeparam>
        /// <param name="service">service name as string</param>
        /// <returns>Generic type parameter</returns>
        public static T Resolve<T>(string service)
        {
            return Container.Resolve<T>(service);
        }

        /// <summary>
        ///     Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>()
        {
            return Container.Resolve<T>();
        }


		/// <summary>
		/// Resolves this instance with constructor arguments
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="constructArgs">Key/Value list of parameter names and values</param>
		/// <returns></returns>
        public static T Resolve<T>(Dictionary<string,object> constructArgs)
		{
			ParameterOverride[] overrides = 
				constructArgs.Select(arg => new ParameterOverride(arg.Key, arg.Value)).ToArray();
			return Container.Resolve<T>(overrides);
        }

		/// <summary>
		/// Resolves this instance with constructor arguments
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name of the mapping to resolve</param>
		/// <param name="constructArgs">Key/Value list of parameter names and values</param>
		/// <returns></returns>
		public static T Resolve<T>(string name, Dictionary<string, object> constructArgs)
		{
			ParameterOverride[] overrides =
				constructArgs.Select(arg => new ParameterOverride(arg.Key, arg.Value)).ToArray();
			return Container.Resolve<T>(name, overrides);
		}

        /// <summary>
        ///     Registers the instance.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="instance">The instance.</param>
        public static void RegisterInstance<T>(string key, T instance)
        {
            Container.RegisterInstance(key, instance);
        }


        /// <summary>
        ///     Registers the instance.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="instance">The instance.</param>
        public static void RegisterInstance(string key, object instance)
        {
            Container.RegisterInstance(key, instance);
        }

        /// <summary>
        ///     Registers the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public static void RegisterInstance<T>(T instance)
        {
            Container.RegisterInstance(instance);
        }
    }
}