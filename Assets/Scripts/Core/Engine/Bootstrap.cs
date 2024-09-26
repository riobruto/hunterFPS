
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Engine
{
    public static class Bootstrap
    {
        private static Dictionary<Type, object> _services { get; set; } = new Dictionary<Type, object>();

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            ///Este metodo es responsable de crear todos los servicios que vamos a utilizar cuando comienza la ejecucion de una escena y los organiza
            ///para poder resolver dependencias unicas de manera ordenada.
            ///
            //Iniciamos todas las clases que sean subclases de Scene Service.

            Type IType = typeof(SceneService);
            IEnumerable<Type> ITypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes()).Where(p => IType.IsAssignableFrom(p));

            foreach (Type type in ITypes)
            {
                SceneService s = (SceneService)Activator.CreateInstance(type);
                _services.Add(type, s);
                s.Initialize();
            }
        }

        public static void Register<T>(T instance)
        {
            _services.Add(typeof(T), instance);
        }

        public static T Resolve<T>()
        {
            return (T)_services[typeof(T)];
        }
    }
}