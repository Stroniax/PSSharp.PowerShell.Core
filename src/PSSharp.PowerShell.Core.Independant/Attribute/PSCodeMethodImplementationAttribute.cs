using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PSSharp
{
    /// <summary>
    /// Identifies the method this attribute is applied to as a CodeMethod for one or more types.
    /// </summary>
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSMethodTarget, AllowMultiple = true, Inherited = false)]
    public sealed class PSCodeMethodImplementationAttribute : PSExtendedTypeDataAttribute
    {
        public PSCodeMethodImplementationAttribute(string typeName, params string[] additionalTypeNames)
        {
            var typeNames = new string[additionalTypeNames.Length + 1];
            typeNames[0] = typeName;
            Array.Copy(additionalTypeNames, 0, typeNames, 1, typeName.Length);
            TypeNames = typeNames;
        }
        public PSCodeMethodImplementationAttribute(Type type, params Type[] additionalTypes)
        {
            var typeNames = new string[additionalTypes.Length + 1];
            typeNames[0] = type.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired);
            for (int i = 0; i < typeNames.Length; i++)
            {
                typeNames[i + 1] = additionalTypes[i].FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired);
            }
            TypeNames = typeNames;
        }
        public IReadOnlyList<string> TypeNames { get; }
        /// <summary>
        /// Override the method name of the generated CodeMethod, instead of using the name of the method
        /// that is referenced.
        /// </summary>
        public string? MethodName { get; set; }

        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var methodImpl = (MethodInfo)attributeTarget;
            var methodName = MethodName ?? methodImpl.Name;
            var codeReference = new CodeReference(methodImpl.DeclaringType?.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired), methodImpl.Name);
            var definitions = new PSExtendedTypeDataDefinition[TypeNames.Count];
            for (int i = 0; i < TypeNames.Count; i++)
            {
                definitions[i] = new PSCodeMethodDefinition(TypeNames[i], methodName, codeReference);
            }
            return definitions;
        }
    }
}
