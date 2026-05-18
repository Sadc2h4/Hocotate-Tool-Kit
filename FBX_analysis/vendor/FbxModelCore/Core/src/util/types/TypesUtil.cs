using System;
using System.Collections.Generic;
using System.Linq;

namespace fin.util.types;

public static class TypesUtil {
  public static IEnumerable<Type> GetAllImplementationTypes<TInterface>()
    => AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(typeof(TInterface).IsAssignableFrom)
                .Where(t => t is {
                    IsAbstract: false, ContainsGenericParameters: false
                });
}