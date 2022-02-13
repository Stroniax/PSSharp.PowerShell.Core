using System;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// A type definition that may be used to generate a TypeData document (such as a .types.ps1xml file)
    /// or directly applied to the type data of a PowerShell session.
    /// <para>This class is a base class for any type data extension for a member, such as a NoteProperty
    /// or ScriptMethod.</para>
    /// </summary>
    public abstract class PSExtendedTypeDataMemberDefinition
        : PSExtendedTypeDataDefinition
    {
        internal PSExtendedTypeDataMemberDefinition(string typeName, string memberName)
            : base(typeName)
        {
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
        }
        public string MemberName { get; }
    }
}