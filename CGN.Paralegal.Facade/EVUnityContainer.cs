using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Configuration;

namespace LexisNexis.Evolution.Delegate
{
    
        public static class EVUnityContainer
        {

            private static IUnityContainer _container = InitializeUnityContainer();
            static IUnityContainer InitializeUnityContainer()
            {
                IUnityContainer container = new UnityContainer();
                UnityConfigurationSection section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
                section.Containers.Default.Configure(container);
                return container;
            }

            public static IUnityContainer Container
            {
                get
                {
                    return _container;
                }
            }
//RI: Do not do anything extensive in static constructors, like reading config files or creating extensive object hierarchies
//  execute this code the first read of Container property     
            //static EVUnityContainer()
            //{
            //    IUnityContainer container = new UnityContainer();
            //    UnityConfigurationSection section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            //    section.Containers.Default.Configure(container);
            //    _container = container;

            //}

            public static T Resolve<T>(string service)
            {
                return _container.Resolve<T>(service);
            }

            //RI: suggest to add this to simplify resolution
            /*
            public static T Resolve<T>()
            {
              return Resolve<T>(typeof(T).Name);
            }
             */ 

        }
    
}
