using System;
using System.Collections.Generic;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell CodeMethod definition.
    /// </summary>
    public sealed class PSCodeMethodDefinition
        : PSExtendedTypeDataMemberDefinition, IEquatable<PSCodeMethodDefinition?>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="methodName"></param>
        /// <param name="codeReference"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSCodeMethodDefinition(
            string typeName,
            string methodName,
            CodeReference codeReference
            )
            : base(typeName, methodName)
        {
            CodeReference = codeReference ?? throw new ArgumentNullException(nameof(codeReference));
        }
        /// <summary>
        /// A reference to the method called by this CodeMethod.
        /// </summary>
        public CodeReference CodeReference { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PSCodeMethodDefinition);
        }

        /// <inheritdoc/>
        public bool Equals(PSCodeMethodDefinition? other)
        {
            return other != null &&
                   TypeName == other.TypeName &&
                   MemberName == other.MemberName &&
                   EqualityComparer<CodeReference>.Default.Equals(CodeReference, other.CodeReference);
        }
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = 1563107199;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MemberName);
            hashCode = hashCode * -1521134295 + EqualityComparer<CodeReference>.Default.GetHashCode(CodeReference);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(PSCodeMethodDefinition? left, PSCodeMethodDefinition? right)
        {
            return EqualityComparer<PSCodeMethodDefinition?>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(PSCodeMethodDefinition? left, PSCodeMethodDefinition? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="memberName"></param>
        /// <param name="codeReference"></param>
        /// <returns></returns>
        public PSCodeMethodDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> memberName = default,
            DefaultOrUpdate<CodeReference> codeReference = default
            ) => new PSCodeMethodDefinition(
                typeName.GetValueOrDefault(TypeName),
                memberName.GetValueOrDefault(MemberName),
                codeReference.GetValueOrDefault(CodeReference)
                );
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>{TypeName}.{MemberName}($args) -> {CodeReference}($this, $args)</returns>
        public override string ToString() => $"{TypeName}.{MemberName}($args) -> {CodeReference}($this, $args)";
    }
}