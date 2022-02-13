using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PSSharp
{
    /// <summary>
    /// Helper class for identifying and using PowerShell type data extensions detailed by
    /// attributes on a given class or member.
    /// </summary>
    public static class PSExtendedTypeDataAnalysis
    {
        internal const AttributeTargets PSPropertyTarget = AttributeTargets.Property | AttributeTargets.Field;
        internal const AttributeTargets PSMethodTarget = AttributeTargets.Method;
        internal const AttributeTargets PSTypeTarget = AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum;




        /// <summary>
        /// Loads type definitions based on attributes applied to a given member.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static List<PSExtendedTypeDataDefinition> GetDefinitions(MemberInfo member, List<PSExtendedTypeDataDefinition>? addToList = null)
        {
            addToList ??= new List<PSExtendedTypeDataDefinition>();

            foreach (var definitionAttribute in member.GetCustomAttributes<PSExtendedTypeDataAttribute>())
            {
                addToList.AddRange(definitionAttribute.GetDefinitions(member));
            }

            return addToList;
        }
        /// <summary>
        /// Loads type definiitons based on attributes applied to a given type and its members.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<PSExtendedTypeDataDefinition> GetDefinitions(Type type, List<PSExtendedTypeDataDefinition>? addToList = null)
        {
            addToList ??= new List<PSExtendedTypeDataDefinition>();

            foreach (var definitionAttribute in type.GetCustomAttributes<PSExtendedTypeDataAttribute>())
            {
                addToList.AddRange(definitionAttribute.GetDefinitions(type));
            }

            foreach (var member in type.GetMembers())
            {
                GetDefinitions(member, addToList);
            }

            return addToList;
        }

        /// <summary>
        /// Loads type definitions based on attributes applied to types in a given assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<PSExtendedTypeDataDefinition> GetDefinitions(Assembly assembly, List<PSExtendedTypeDataDefinition>? addToList = null)
        {
            addToList ??= new List<PSExtendedTypeDataDefinition>();
            foreach (var type in assembly.GetExportedTypes())
            {
                GetDefinitions(type, addToList);
            }
            return addToList;
        }

        /// <summary>
        /// Loads type definitions based on attributes applied to types in all assemblies in the app domain.
        /// </summary>
        /// <returns></returns>
        public static List<PSExtendedTypeDataDefinition> GetDefinitionsInAppDomain()
        {
            var total = new List<PSExtendedTypeDataDefinition>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic)
                {
                    GetDefinitions(assembly, total);
                }
            }
            return total;
        }
    }
}