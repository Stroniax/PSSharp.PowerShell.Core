using System;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// A type definition that may be used to generate a TypeData document (such as a .types.ps1xml file)
    /// or directly applied to the type data of a PowerShell session.
    /// </summary>
    [PSScriptProperty("Type", "$this.TypeName -as [Type]")]
    public abstract class PSExtendedTypeDataDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal PSExtendedTypeDataDefinition(string typeName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }
        /// <summary>
        /// The type name that the type data applies to.
        /// </summary>
        public string TypeName { get; }
    }
}