using System;
using System.Collections.Generic;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell ScriptMethod definition.
    /// </summary>
    [PSScriptProperty("Script", "[ScriptBlock]::Create($this.ScriptText)")]
    public sealed class PSScriptMethodDefinition
        : PSExtendedTypeDataMemberDefinition, IEquatable<PSScriptMethodDefinition?>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="memberName"></param>
        /// <param name="scriptText"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSScriptMethodDefinition(
            string typeName,
            string memberName,
            string scriptText
            )
            : base(typeName, memberName)
        {
            ScriptText = scriptText ?? throw new ArgumentNullException(nameof(ScriptText));
        }
        public string ScriptText { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PSScriptMethodDefinition);
        }

        /// <inheritdoc/>
        public bool Equals(PSScriptMethodDefinition? other)
        {
            return other != null &&
                   TypeName == other.TypeName &&
                   MemberName == other.MemberName &&
                   ScriptText == other.ScriptText;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = 830773943;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MemberName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ScriptText);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(PSScriptMethodDefinition? left, PSScriptMethodDefinition? right)
        {
            return EqualityComparer<PSScriptMethodDefinition?>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(PSScriptMethodDefinition? left, PSScriptMethodDefinition? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        public PSScriptMethodDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> memberName = default,
            DefaultOrUpdate<string> scriptText = default
            ) => new PSScriptMethodDefinition(
                typeName.GetValueOrDefault(TypeName),
                memberName.GetValueOrDefault(MemberName),
                scriptText.GetValueOrDefault(ScriptText)
                );
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>{TypeName}.{MemberName} = {<br/>{ScriptText}<br/>}</returns>
        public override string ToString() => $"{TypeName}.{MemberName} = {{\n{ScriptText}\n}}";
    }
}