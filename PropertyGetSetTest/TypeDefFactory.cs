/*
 * Author：Yuzhao Bai
 * Email: yuzhaobai@outlook.com
 * The code of this file and others in HB.FullStack.* are licensed under MIT LICENSE.
 */


using System.Reflection;
using System.Collections.Concurrent;

namespace System
{
    public static class TypeDefFactory
    {
        private static ConcurrentDictionary<Type, TypeDef> _typeDefs = new ConcurrentDictionary<Type, TypeDef>();

        public static TypeDef GetTypeDef<T>() => CreateTypeDef(typeof(T));

        public static TypeDef GetTypeDef(Type type)
        {
            return _typeDefs.GetOrAdd(type, _ => CreateTypeDef(type));
        }

        public static PropertyDef? GetPropertyDef<T>(string propertyName)
        {
            TypeDef typeDef = GetTypeDef<T>();
            return typeDef.GetPropertyDef(propertyName);
        }

        private static TypeDef CreateTypeDef(Type type)
        {
            TypeDef typeDef = new TypeDef
            {
                FullName = type.FullName!
            };

            typeDef.Type = type;

            foreach (PropertyInfo property in type.GetProperties())
            {
                PropertyDef propertyDef = new PropertyDef();
                propertyDef.PropertyName = property.Name;
                propertyDef.PropertyType = property.GetType();
                //propertyDef.GetMethod = property.GetGetMethod();
                //propertyDef.SetMethod = property.GetSetMethod();
                propertyDef.GetMethod = type.GetMethod(string.Format("get_{0}", property.Name));
                propertyDef.SetMethod = type.GetMethod(String.Format("set_{0}", property.Name));

                typeDef.PropertyDefs[propertyDef.PropertyName] = propertyDef;
            }

            return typeDef;
        }
    }
}