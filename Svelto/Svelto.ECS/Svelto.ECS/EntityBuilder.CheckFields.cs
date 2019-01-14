﻿#if !DEBUG || PROFILER
#define DISABLE_CHECKS
using System.Diagnostics;
#endif    
using System;
using System.Reflection;
using Svelto.ECS.Internal;

namespace Svelto.ECS
{
    public partial class EntityBuilder<T>
    {
#if DISABLE_CHECKS        
        [Conditional("_CHECKS_DISABLED")]
#endif
        static void CheckInvalidFields(Type type, bool needsReflection, bool isRoot)
        {
            if (ENTITY_VIEW_TYPE == typeof(EntityInfoView) || (type == EGIDType || type == ExclusiveGroupStructType)) 
                return;

            {
                var methods = type.GetMethods(BindingFlags.Public   |
                                              BindingFlags.Instance | BindingFlags.DeclaredOnly);

                var properties = type.GetProperties(BindingFlags.Public   |
                                                    BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (isRoot)
                {
                    if (properties.Length > 1)
                        ProcessError("EntityViews cannot have public methods or properties", type);
                        
                    if (methods.Length > properties.Length + 1)
                        ProcessError("EntityViews cannot have public methods or properties", type);
                }
                else
                {
                    if (properties.Length > 0)
                        ProcessError("Entity components fields cannot have public methods or properties", type);

                    if (methods.Length > 0)
                        ProcessError("Entity components fields cannot have public methods or properties", type);
                }
            }

            if (needsReflection == false)
            {
                if (type.IsClass)
                    throw new ECSException("IEntityStructs must be structs - EntityView: ".FastConcat(ENTITY_VIEW_TYPE));

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                for (int i = fields.Length - 1; i >= 0; --i)
                {
                    var field = fields[i];
                    var fieldFieldType = field.FieldType;

                    SubCheckFields(fieldFieldType);
                }
            }
            else
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                if (fields.Length < 1)
                    ProcessError("Entity View Structs must hold entity components interfaces", type);
                
                for (int i = fields.Length - 1; i >= 0; --i)
                {
                    var field = fields[i];
                    
                    if (field.FieldType.IsInterfaceEx() == false)
                        ProcessError("Entity View Structs must hold entity components interfaces", type);
                    
                    var properties = field.FieldType.GetProperties(BindingFlags.Public |
                                                        BindingFlags.Instance | BindingFlags.DeclaredOnly);

                    for (int j = properties.Length - 1; j >= 0; --j)
                    {
                        if (properties[j].PropertyType.IsGenericType == true)
                        {
                            var genericTypeDefinition = properties[j].PropertyType.GetGenericTypeDefinition();
                            if (genericTypeDefinition == typeof(DispatchOnSet<>) ||
                                genericTypeDefinition == typeof(DispatchOnChange<>)) continue;
                        }
                        
                        SubCheckFields(properties[j].PropertyType);
                    }
                }
            }
        }

        static void SubCheckFields(Type fieldFieldType)
        {
            if (fieldFieldType.IsPrimitive == true || fieldFieldType.IsValueType == true)
            {
                if (fieldFieldType.IsValueType && !fieldFieldType.IsEnum && fieldFieldType.IsPrimitive == false)
                {
                    CheckInvalidFields(fieldFieldType, false, false);
                }

                return;
            }
            
            ProcessError("Entity Structs field and Entity View Struct components must hold value types", fieldFieldType);
        }
        
        static void ProcessError(string message, Type type)
        {
#if !RELAXED_ECS
            throw new EntityStructException(message, ENTITY_VIEW_TYPE, type);
#endif
        }
        
        static void CheckFieldsForDataBinding(Type entityViewType)
        {
            if (ENTITY_VIEW_TYPE == typeof(EntityInfoView) ||
                (entityViewType == EGIDType || entityViewType == ExclusiveGroupStructType)) 
                return;

            _ATTRIBUTES = entityViewType.GetCustomAttributes<DataBindsToAttribute>();
        }

        static readonly Type EGIDType                 = typeof(EGID);
        static readonly Type ExclusiveGroupStructType = typeof(ExclusiveGroup.ExclusiveGroupStruct);
    }
    
    public class EntityStructException : Exception
    {
        public EntityStructException(string message, Type entityViewType, Type type):
            base(message.FastConcat(" entity view: ", entityViewType.ToString(), " field: ", type.ToString()))
        {}
    }
}