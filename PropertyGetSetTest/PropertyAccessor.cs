/*
 * Author：Yuzhao Bai
 * Email: yuzhaobai@outlook.com
 * The code of this file and others in HB.FullStack.* are licensed under MIT LICENSE.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace System
{
    public static class PropertyAccessor
    {
        private static Type _objectType = typeof(object);
        private static Type _stringType = typeof(string);

        private static object _locker = new object();
        private static Hashtable _setActions = new Hashtable();
        //private static Dictionary<string, Action<object, object?>> _setActions = new Dictionary<string, Action<object, object?>>();

        public static Action<object, object?> GetSetAction(Type type, string propertyName)
        {
            //TypeDef typeDef = TypeDefFactory.GetTypeDef(type);
            //PropertyDef? propertyDef = typeDef.GetPropertyDef(propertyName);

            //if (propertyDef == null)
            //{
            //    throw new ArgumentException($"{typeDef.FullName} do not have a {propertyName}", nameof(propertyName));
            //}

            //if (propertyDef.SetMethod == null)
            //{
            //    throw new ArgumentException($"{typeDef.FullName}.{propertyName} do not have a Setter.", nameof(propertyName));
            //}

            string key = type.FullName + propertyName;

            Action<object, object?>? action = (Action<object, object?>)_setActions[key];

            if (action == null)
            {
                lock (_locker)
                {
                    action = (Action<object, object?>)_setActions[key];

                    if (action == null)
                    {
                        action = CreatePropertySetDelegate(type, propertyName);
                        _setActions[key] = action;
                    }
                }
            }

            //if (!_setActions.TryGetValue(key, out Action<object, object?>? action))
            //{
            //    lock (_locker)
            //    {
            //        if (!_setActions.TryGetValue(key, out action))
            //        {
            //            action = CreatePropertySetDelegate(type, propertyName);
            //            _setActions[key] = action;
            //        }
            //    }
            //}

            return action;
        }

        public static void Set(Type type, object obj, string propertyName, object? propertyValue)
        {
            //TypeDef typeDef = TypeDefFactory.GetTypeDef(type);
            //PropertyDef? propertyDef = typeDef.GetPropertyDef(propertyName);

            //if (propertyDef == null)
            //{
            //    throw new ArgumentException($"{typeDef.FullName} do not have a {propertyName}", nameof(propertyName));
            //}

            //if (propertyDef.SetMethod == null)
            //{
            //    throw new ArgumentException($"{typeDef.FullName}.{propertyName} do not have a Setter.", nameof(propertyName));
            //}

            Action<object, object?> action = GetSetAction(type, propertyName);

            action.Invoke(obj, propertyValue);
        }

        //classValue - propertyValue 
        public static Action<object, object?> CreatePropertySetDelegate(Type type, string propertyName)
        {
            DynamicMethod dm = new DynamicMethod(
                "PropertySet" + Guid.NewGuid().ToString(),
                null,
                new[] { _objectType, _objectType },
                true);

            ILGenerator il = dm.GetILGenerator();

            EmitPropertySet(il, type, propertyName);

            Type actionType = Expression.GetActionType(_objectType, _objectType);
            return (Action<object, object?>)dm.CreateDelegate(actionType);
        }

        //arg1 : class value
        //arg2 : property value
        //fixed : class Type, property Type, propertyName
        private static void EmitPropertySet(ILGenerator il, Type type, string propertyName)
        {
            //nameSetMethod.Invoke(args1, new object[] { arg2 });

            //LocalBuilder modelTypeLocal = il.DeclareLocal(typeof(Type));

            il.Emit(OpCodes.Ldarg_0);//[objValue]

            //il.Emit(OpCodes.Castclass, type); //[typeValue]

            il.Emit(OpCodes.Ldarg_1);//[typeValue][propertyValue]

            PropertyInfo propertyInfo = type.GetProperty(propertyName)!;

            //il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);//[typeValue][typePropertyValue]

            //===赋值================================================================================
            // stack is now [arg1][arg2][TypeValue]

            //if (propertyDef.PropertyType.IsValueType)
            //{
            //    il.EmitCall(OpCodes.Call, propertyDef.SetMethod!, null);
            //}
            //else
            //{
            //    il.EmitCall(OpCodes.Callvirt, propertyDef.SetMethod!, null);
            //}

            MethodInfo setMethod = propertyInfo.GetSetMethod()!;

            il.EmitCall(OpCodes.Callvirt, setMethod, null);

            il.Emit(OpCodes.Ret);

        }

        private static void EmitPropertySet2(ILGenerator il, Type type, string propertyName)
        {
            //nameSetMethod.Invoke(args1, new object[] { arg2 });

            LocalBuilder modelTypeLocal = il.DeclareLocal(typeof(Type));

            il.Emit(OpCodes.Ldarg_0);//[objValue]

            //Why need cast ?
            il.Emit(OpCodes.Castclass, type); //[typeValue]

            il.Emit(OpCodes.Ldarg_1);//[typeValue][propertyValue]

            PropertyInfo propertyInfo = type.GetProperty(propertyName)!;
            MethodInfo setMethod = propertyInfo.GetSetMethod()!;

            //why ?
            il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);//[typeValue][typePropertyValue]

            //===赋值================================================================================
            // stack is now [arg1][arg2][TypeValue]

            if (propertyInfo.PropertyType.IsValueType)
            {
                il.EmitCall(OpCodes.Call, setMethod!, null);
            }
            else
            {
                il.EmitCall(OpCodes.Callvirt, setMethod!, null);
            }

            il.EmitCall(OpCodes.Callvirt, setMethod, null);

            il.Emit(OpCodes.Ret);

        }

    }
}