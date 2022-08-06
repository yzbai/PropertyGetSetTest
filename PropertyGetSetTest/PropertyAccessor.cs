/*
 * Author：Yuzhao Bai
 * Email: yuzhaobai@outlook.com
 * The code of this file and others in HB.FullStack.* are licensed under MIT LICENSE.
 */


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Collections;

namespace System
{
    public static class PropertyAccessor
    {
        private static Type _objectType = typeof(object);
        private static Type _stringType = typeof(string);

        private static object _locker = new object();
        //private static Hashtable _setActions = new Hashtable();
        private static Dictionary<string, Action<object, object?>> _setActions = new Dictionary<string, Action<object, object?>>();

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

            if (!_setActions.TryGetValue(key, out Action<object, object?>? action))
            {
                lock (_locker)
                {
                    if (!_setActions.TryGetValue(key, out action))
                    {
                        action = CreatePropertySetDelegate(type, propertyName);
                        _setActions[key] = action;
                    }
                }
            }

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

            string key = type.FullName+propertyName;

            if(!_setActions.TryGetValue(key, out Action<object, object?>? action))
            {
                lock(_locker)
                {
                    if (!_setActions.TryGetValue(key, out  action))
                    {
                        action = CreatePropertySetDelegate(type, propertyName);
                        _setActions[key] = action;
                    }
                }
            }

            action.Invoke(obj, propertyName);
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
        /*
               /// <summary>
               /// 得到一个将 (IDBModelDefFactory,Model,parameter_num_suffix)转换为键值对的delegate
               /// </summary>
               /// <param name="modelDef"></param>
               /// <param name="engineType"></param>
               /// <returns></returns>
               public static Func<IDBModelDefFactory, object, int, KeyValuePair<string, object>[]> CreateModelToParametersDelegate(DBModelDef modelDef, EngineType engineType)
               {
                   DynamicMethod dm = new DynamicMethod("ModelToParameters" + Guid.NewGuid().ToString(), typeof(KeyValuePair<string, object>[]), new[] { typeof(IDBModelDefFactory), typeof(object), typeof(int) }, true);
                   ILGenerator il = dm.GetILGenerator();

                   LocalBuilder array = il.DeclareLocal(typeof(KeyValuePair<string, object>[]));
                   LocalBuilder tmpObj = il.DeclareLocal(typeof(object));
                   LocalBuilder modelTypeLocal = il.DeclareLocal(typeof(Type));
                   LocalBuilder tmpTrueTypeLocal = il.DeclareLocal(typeof(Type));
                   LocalBuilder modelLocal = il.DeclareLocal(modelDef.ModelType);
                   LocalBuilder numberLocal = il.DeclareLocal(typeof(object));

                   il.Emit(OpCodes.Ldarg_1);
                   il.Emit(OpCodes.Unbox_Any, modelDef.ModelType);
                   il.Emit(OpCodes.Stloc, modelLocal);

                   il.Emit(OpCodes.Ldarg_2);
                   il.Emit(OpCodes.Box, typeof(int));
                   il.Emit(OpCodes.Stloc, numberLocal);

                   il.Emit(OpCodes.Ldtoken, modelDef.ModelType);
                   il.EmitCall(OpCodes.Call, _getTypeFromHandleMethod, null);
                   il.Emit(OpCodes.Stloc, modelTypeLocal);

                   EmitInt32(il, modelDef.FieldCount);
                   il.Emit(OpCodes.Newarr, typeof(KeyValuePair<string, object>));
                   il.Emit(OpCodes.Stloc, array);

                   int index = 0;
                   foreach (var propertyDef in modelDef.PropertyDefs)
                   {
                       Label nullLabel = il.DefineLabel();
                       Label finishLabel = il.DefineLabel();

                       il.Emit(OpCodes.Ldloc, array);//[rtArray]

                       il.Emit(OpCodes.Ldstr, $"{propertyDef.DbParameterizedName!}_");

                       il.Emit(OpCodes.Ldloc, numberLocal);

                       il.EmitCall(OpCodes.Call, _getStringConcatMethod, null);//[rtArray][key]

                       il.Emit(OpCodes.Ldloc, modelLocal);//[rtArray][key][model]

                       if (propertyDef.Type.IsValueType)
                       {
                           il.EmitCall(OpCodes.Call, propertyDef.GetMethod, null);
                           il.Emit(OpCodes.Box, propertyDef.Type);
                       }
                       else
                       {
                           il.EmitCall(OpCodes.Callvirt, propertyDef.GetMethod, null);
                       }

                       //[rtArray][key][property_value_obj]

                       #region TypeValue To DbValue

                       //判断是否是null
                       il.Emit(OpCodes.Dup);//[rtArray][key][property_value_obj][property_value_obj]
                       il.Emit(OpCodes.Brfalse_S, nullLabel);//[rtArray][key][property_value_obj]

                       if (propertyDef.TypeConverter != null)
                       {
                           il.Emit(OpCodes.Stloc, tmpObj);//[rtArray][key]

                           il.Emit(OpCodes.Ldarg_0); //[rtArray][key][IDBModelDefFactory]

                           il.Emit(OpCodes.Ldloc, modelTypeLocal);//[rtArray][key][IDBModelDefFactory][modelType]
                           //emiter.LoadLocal(modelTypeLocal);

                           il.Emit(OpCodes.Ldstr, propertyDef.Name);//[rtArray][key][IDBModelDefFactory][modelType][propertyName]
                           il.EmitCall(OpCodes.Callvirt, _getPropertyTypeConverterMethod2, null);//[rtArray][key][typeconverter]

                           il.Emit(OpCodes.Ldloc, tmpObj);
                           //emiter.LoadLocal(tmpObj);
                           //[rtArray][key][typeconveter][property_value_obj]
                           il.Emit(OpCodes.Ldtoken, propertyDef.Type);
                           //emiter.LoadConstant(propertyDef.Type);
                           il.EmitCall(OpCodes.Call, _getTypeFromHandleMethod, null);
                           //emiter.Call(ModelMapperDelegateCreator._getTypeFromHandleMethod);
                           //[rtArray][key][typeconveter][property_value_obj][property_type]
                           il.EmitCall(OpCodes.Callvirt, _getTypeConverterTypeValueToDbValueMethod, null);
                           //emiter.CallVirtual(typeof(ITypeConverter).GetMethod(nameof(ITypeConverter.TypeValueToDbValue)));
                           //[rtArray][key][db_value]
                       }
                       else
                       {
                           Type trueType = propertyDef.NullableUnderlyingType ?? propertyDef.Type;

                           //查看全局TypeConvert

                           ITypeConverter? globalConverter = TypeConvert.GetGlobalTypeConverter(trueType, engineType);

                           if (globalConverter != null)
                           {
                               il.Emit(OpCodes.Stloc, tmpObj);
                               //emiter.StoreLocal(tmpObj);
                               //[rtArray][key]

                               il.Emit(OpCodes.Ldtoken, trueType);
                               //emiter.LoadConstant(trueType);
                               il.EmitCall(OpCodes.Call, _getTypeFromHandleMethod, null);
                               //emiter.Call(ModelMapperDelegateCreator._getTypeFromHandleMethod);
                               il.Emit(OpCodes.Stloc, tmpTrueTypeLocal);
                               //emiter.StoreLocal(tmpTrueTypeLocal);
                               il.Emit(OpCodes.Ldloc, tmpTrueTypeLocal);
                               //emiter.LoadLocal(tmpTrueTypeLocal);

                               EmitInt32(il, (int)engineType);
                               //emiter.LoadConstant((int)engineType);
                               il.EmitCall(OpCodes.Call, _getGlobalTypeConverterMethod, null);
                               //emiter.Call(ModelMapperDelegateCreator._getGlobalTypeConverterMethod);
                               //[rtArray][key][typeconverter]

                               il.Emit(OpCodes.Ldloc, tmpObj);
                               //emiter.LoadLocal(tmpObj);
                               //[rtArray][key][typeconverter][property_value_obj]
                               il.Emit(OpCodes.Ldloc, tmpTrueTypeLocal);
                               //emiter.LoadLocal(tmpTrueTypeLocal);
                               //[rtArray][key][typeconverter][property_value_obj][true_type]
                               il.EmitCall(OpCodes.Callvirt, _getTypeConverterTypeValueToDbValueMethod, null);
                               //emiter.CallVirtual(typeof(ITypeConverter).GetMethod(nameof(ITypeConverter.TypeValueToDbValue)));
                               //[rtArray][key][db_value]
                           }
                           else
                           {
                               //默认
                               if (trueType.IsEnum)
                               {
                                   il.EmitCall(OpCodes.Callvirt, _getObjectToStringMethod, null);
                                   //emiter.CallVirtual(_getObjectToStringMethod);
                               }
                           }
                       }

                       il.Emit(OpCodes.Br_S, finishLabel);
                       ////emiter.Branch(finishLabel);

                       #endregion

                       #region If Null

                       il.MarkLabel(nullLabel);
                       //emiter.MarkLabel(nullLabel);
                       //[rtArray][key][property_value_obj]

                       il.Emit(OpCodes.Pop);
                       //emiter.Pop();
                       //[rtArray][key]

                       il.Emit(OpCodes.Ldsfld, _dbNullValueFiled);
                       //emiter.LoadField(typeof(DBNull).GetField("Value"));
                       //[rtArray][key][DBNull]

                       //il.Emit(OpCodes.Br_S, finishLabel);
                       ////emiter.Branch(finishLabel);

                       #endregion

                       il.MarkLabel(finishLabel);
                       ////emiter.MarkLabel(finishLabel);

                       var kvCtor = typeof(KeyValuePair<string, object>).GetConstructor(new Type[] { typeof(string), typeof(object) })!;

                       il.Emit(OpCodes.Newobj, kvCtor);
                       //emiter.NewObject(kvCtor);
                       //[rtArray][kv]

                       il.Emit(OpCodes.Box, typeof(KeyValuePair<string, object>));
                       //emiter.Box<KeyValuePair<string, object>>();
                       //[rtArray][kv_obj]

                       EmitInt32(il, index);
                       //emiter.LoadConstant(index);
                       //[rtArray][kv_obj][index]

                       il.EmitCall(OpCodes.Call, _getArraySetValueMethod, null);
                       //emiter.Call(typeof(Array).GetMethod(nameof(Array.SetValue), new Type[] { typeof(object), typeof(int) }));

                       index++;
                   }

                   il.Emit(OpCodes.Ldloc, array);
                   //emiter.LoadLocal(rtArray);

                   il.Emit(OpCodes.Ret);
                   //emiter.Return();

                   Type funType = Expression.GetFuncType(typeof(IDBModelDefFactory), typeof(object), typeof(int), typeof(KeyValuePair<string, object>[]));

                   return (Func<IDBModelDefFactory, object, int, KeyValuePair<string, object>[]>)dm.CreateDelegate(funType);

                   //return emiter.CreateDelegate();
               }

               /// <summary>
               /// 固定了PropertyNames的顺序，做cache时，要做顺序
               /// </summary>
               /// <param name="modelDef"></param>
               /// <param name="engineType"></param>
               /// <param name="propertyNames"></param>
               /// <returns></returns>

               public static Func<IDBModelDefFactory, object?[], string, KeyValuePair<string, object>[]> CreatePropertyValuesToParametersDelegate(DBModelDef modelDef, EngineType engineType, IList<string> propertyNames)
               {
                   DynamicMethod dm = new DynamicMethod(
                       "PropertyValuesToParameters" + Guid.NewGuid().ToString(),
                       typeof(KeyValuePair<string, object>[]),
                       new[]
                       {
                           typeof(IDBModelDefFactory),
                           typeof(object?[]), //propertyValues
                           typeof(string) //parameterNameSuffix
                       },
                       true);

                   ILGenerator il = dm.GetILGenerator();

                   LocalBuilder rtArray = il.DeclareLocal(typeof(KeyValuePair<string, object>[]));
                   LocalBuilder tmpObj = il.DeclareLocal(typeof(object));
                   LocalBuilder modelTypeLocal = il.DeclareLocal(typeof(Type));
                   LocalBuilder tmpTrueTypeLocal = il.DeclareLocal(typeof(Type));
                   //LocalBuilder modelLocal = il.DeclareLocal(modelDef.ModelType);
                   LocalBuilder propertyValues = il.DeclareLocal(typeof(object?[]));
                   LocalBuilder parameterSuffixLocal = il.DeclareLocal(typeof(string));

                   //il.Emit(OpCodes.Ldarg_1);
                   //il.Emit(OpCodes.Unbox_Any, modelDef.ModelType);
                   //il.Emit(OpCodes.Stloc, modelLocal);

                   il.Emit(OpCodes.Ldarg_1);
                   il.Emit(OpCodes.Stloc, propertyValues);

                   il.Emit(OpCodes.Ldarg_2);
                   //il.Emit(OpCodes.Box, typeof(string));
                   il.Emit(OpCodes.Stloc, parameterSuffixLocal);

                   il.Emit(OpCodes.Ldtoken, modelDef.ModelType);
                   il.EmitCall(OpCodes.Call, _getTypeFromHandleMethod, null);
                   il.Emit(OpCodes.Stloc, modelTypeLocal);

                   EmitInt32(il, propertyNames.Count);
                   il.Emit(OpCodes.Newarr, typeof(KeyValuePair<string, object>));
                   il.Emit(OpCodes.Stloc, rtArray);

                   int index = 0;
                   foreach (string propertyName in propertyNames)
                   {
                       DBModelPropertyDef? propertyDef = modelDef.GetPropertyDef(propertyName);

                       if (propertyDef == null)
                       {
                           throw DatabaseExceptions.PropertyNotFound(modelDef.ModelFullName, propertyName);
                       }

                       Label nullLabel = il.DefineLabel();
                       Label finishLabel = il.DefineLabel();

                       il.Emit(OpCodes.Ldloc, rtArray);//[rtArray]

                       il.Emit(OpCodes.Ldstr, $"{propertyDef.DbParameterizedName!}_");

                       il.Emit(OpCodes.Ldloc, parameterSuffixLocal);

                       il.EmitCall(OpCodes.Call, _getStringConcatMethod, null);//[rtArray][key]

                       il.Emit(OpCodes.Ldloc, propertyValues);//[rtArray][key][propetyValues]

                       EmitInt32(il, index); //[rtArray][key][propetyValues][index]

                       il.EmitCall(OpCodes.Call, _getArrayGetValueMethod, null); //[rtArray][key][property_value_obj]

                       //if (propertyDef.Type.IsValueType)
                       //{
                       //    il.EmitCall(OpCodes.Call, propertyDef.GetMethod, null);
                       //    il.Emit(OpCodes.Box, propertyDef.Type);
                       //}
                       //else
                       //{
                       //    il.EmitCall(OpCodes.Callvirt, propertyDef.GetMethod, null);
                       //}

                       //[rtArray][key][property_value_obj]

                       #region TypeValue To DbValue

                       //判断是否是null
                       il.Emit(OpCodes.Dup);//[rtArray][key][property_value_obj][property_value_obj]
                       il.Emit(OpCodes.Brfalse_S, nullLabel);//[rtArray][key][property_value_obj]

                       if (propertyDef.TypeConverter != null)
                       {
                           il.Emit(OpCodes.Stloc, tmpObj);//[rtArray][key]

                           il.Emit(OpCodes.Ldarg_0); //[rtArray][key][IDBModelDefFactory]

                           il.Emit(OpCodes.Ldloc, modelTypeLocal);//[rtArray][key][IDBModelDefFactory][modelType]
                           //emiter.LoadLocal(modelTypeLocal);

                           il.Emit(OpCodes.Ldstr, propertyDef.Name);//[rtArray][key][IDBModelDefFactory][modelType][propertyName]
                           il.EmitCall(OpCodes.Callvirt, _getPropertyTypeConverterMethod2, null);//[rtArray][key][typeconverter]

                           il.Emit(OpCodes.Ldloc, tmpObj);//[rtArray][key][typeconveter][property_value_obj]

                           il.Emit(OpCodes.Ldtoken, propertyDef.Type);
                           //emiter.LoadConstant(propertyDef.Type);
                           il.EmitCall(OpCodes.Call, _getTypeFromHandleMethod, null);
                           //emiter.Call(ModelMapperDelegateCreator._getTypeFromHandleMethod);
                           //[rtArray][key][typeconveter][property_value_obj][property_type]
                           il.EmitCall(OpCodes.Callvirt, _getTypeConverterTypeValueToDbValueMethod, null);
                           //emiter.CallVirtual(typeof(ITypeConverter).GetMethod(nameof(ITypeConverter.TypeValueToDbValue)));
                           //[rtArray][key][db_value]
                       }
                       else
                       {
                           Type trueType = propertyDef.NullableUnderlyingType ?? propertyDef.Type;

                           //查看全局TypeConvert

                           ITypeConverter? globalConverter = TypeConvert.GetGlobalTypeConverter(trueType, engineType);

                           if (globalConverter != null)
                           {
                               il.Emit(OpCodes.Stloc, tmpObj);
                               //emiter.StoreLocal(tmpObj);
                               //[rtArray][key]

                               il.Emit(OpCodes.Ldtoken, trueType);
                               //emiter.LoadConstant(trueType);
                               il.EmitCall(OpCodes.Call, _getTypeFromHandleMethod, null);
                               //emiter.Call(ModelMapperDelegateCreator._getTypeFromHandleMethod);
                               il.Emit(OpCodes.Stloc, tmpTrueTypeLocal);
                               //emiter.StoreLocal(tmpTrueTypeLocal);
                               il.Emit(OpCodes.Ldloc, tmpTrueTypeLocal);
                               //emiter.LoadLocal(tmpTrueTypeLocal);

                               EmitInt32(il, (int)engineType);
                               //emiter.LoadConstant((int)engineType);
                               il.EmitCall(OpCodes.Call, _getGlobalTypeConverterMethod, null);
                               //emiter.Call(ModelMapperDelegateCreator._getGlobalTypeConverterMethod);
                               //[rtArray][key][typeconverter]

                               il.Emit(OpCodes.Ldloc, tmpObj);
                               //emiter.LoadLocal(tmpObj);
                               //[rtArray][key][typeconverter][property_value_obj]
                               il.Emit(OpCodes.Ldloc, tmpTrueTypeLocal);
                               //emiter.LoadLocal(tmpTrueTypeLocal);
                               //[rtArray][key][typeconverter][property_value_obj][true_type]
                               il.EmitCall(OpCodes.Callvirt, _getTypeConverterTypeValueToDbValueMethod, null);
                               //emiter.CallVirtual(typeof(ITypeConverter).GetMethod(nameof(ITypeConverter.TypeValueToDbValue)));
                               //[rtArray][key][db_value]
                           }
                           else
                           {
                               //默认
                               if (trueType.IsEnum)
                               {
                                   il.EmitCall(OpCodes.Callvirt, _getObjectToStringMethod, null);
                                   //emiter.CallVirtual(_getObjectToStringMethod);
                               }
                           }
                       }

                       il.Emit(OpCodes.Br_S, finishLabel);
                       ////emiter.Branch(finishLabel);

                       #endregion

                       #region If Null

                       il.MarkLabel(nullLabel);
                       //emiter.MarkLabel(nullLabel);
                       //[rtArray][key][property_value_obj]

                       il.Emit(OpCodes.Pop);
                       //emiter.Pop();
                       //[rtArray][key]

                       il.Emit(OpCodes.Ldsfld, _dbNullValueFiled);
                       //emiter.LoadField(typeof(DBNull).GetField("Value"));
                       //[rtArray][key][DBNull]

                       //il.Emit(OpCodes.Br_S, finishLabel);
                       ////emiter.Branch(finishLabel);

                       #endregion

                       il.MarkLabel(finishLabel);
                       ////emiter.MarkLabel(finishLabel);

                       var kvCtor = typeof(KeyValuePair<string, object>).GetConstructor(new Type[] { typeof(string), typeof(object) })!;

                       il.Emit(OpCodes.Newobj, kvCtor);
                       //emiter.NewObject(kvCtor);
                       //[rtArray][kv]

                       il.Emit(OpCodes.Box, typeof(KeyValuePair<string, object>));
                       //emiter.Box<KeyValuePair<string, object>>();
                       //[rtArray][kv_obj]

                       EmitInt32(il, index);
                       //emiter.LoadConstant(index);
                       //[rtArray][kv_obj][index]

                       il.EmitCall(OpCodes.Call, _getArraySetValueMethod, null);
                       //emiter.Call(typeof(Array).GetMethod(nameof(Array.SetValue), new Type[] { typeof(object), typeof(int) }));

                       index++;
                   }

                   il.Emit(OpCodes.Ldloc, rtArray);
                   //emiter.LoadLocal(rtArray);

                   il.Emit(OpCodes.Ret);
                   //emiter.Return();

                   Type funType = Expression.GetFuncType(typeof(IDBModelDefFactory), typeof(object?[]), typeof(string), typeof(KeyValuePair<string, object>[]));

                   return (Func<IDBModelDefFactory, object?[], string, KeyValuePair<string, object>[]>)dm.CreateDelegate(funType);

                   //return emiter.CreateDelegate();
               }
               */
    }
}