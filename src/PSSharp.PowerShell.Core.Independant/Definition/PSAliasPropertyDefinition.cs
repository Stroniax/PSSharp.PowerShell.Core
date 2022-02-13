using System;
using System.Collections.Generic;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell AliasProperty definition.
    /// </summary>
    public sealed class PSAliasPropertyDefinition
        : PSExtendedTypeDataMemberDefinition, IEquatable<PSAliasPropertyDefinition?>
    {
        public PSAliasPropertyDefinition(
            string typeName,
            string memberName,
            string referencedMemberName
            ) : base(typeName, memberName)
        {
            ReferencedMemberName = referencedMemberName ?? throw new ArgumentNullException(nameof(referencedMemberName));
        }

        /// <summary>
        /// The property this alias property refers to.
        /// </summary>
        public string ReferencedMemberName { get; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as PSAliasPropertyDefinition);
        }

        public bool Equals(PSAliasPropertyDefinition? other)
        {
            return other != null &&
                   TypeName == other.TypeName &&
                   MemberName == other.MemberName &&
                   ReferencedMemberName == other.ReferencedMemberName;
        }

        public override int GetHashCode()
        {
            int hashCode = 1561705125;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MemberName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ReferencedMemberName);
            return hashCode;
        }

        public static bool operator ==(PSAliasPropertyDefinition? left, PSAliasPropertyDefinition? right)
        {
            return EqualityComparer<PSAliasPropertyDefinition?>.Default.Equals(left, right);
        }

        public static bool operator !=(PSAliasPropertyDefinition? left, PSAliasPropertyDefinition? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        public PSAliasPropertyDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName,
            DefaultOrUpdate<string> memberName,
            DefaultOrUpdate<string> referencedMemberName
            ) => new PSAliasPropertyDefinition(
                typeName.GetValueOrDefault(TypeName),
                memberName.GetValueOrDefault(MemberName),
                referencedMemberName.GetValueOrDefault(ReferencedMemberName)
                );
        public override string ToString() => $"{TypeName}.{ReferencedMemberName} -> {TypeName}.{MemberName}";
    }
}