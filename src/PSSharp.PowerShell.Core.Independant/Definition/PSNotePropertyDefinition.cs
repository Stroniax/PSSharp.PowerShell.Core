using System;
using System.Collections.Generic;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell NoteProperty definition.
    /// </summary>
    public sealed class PSNotePropertyDefinition
        : PSExtendedTypeDataMemberDefinition, IEquatable<PSNotePropertyDefinition?>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="memberName"></param>
        /// <param name="value"></param>
        public PSNotePropertyDefinition(
            string typeName,
            string memberName,
            object? value
            )
            : base(typeName, memberName)
        {
            Value = value;
        }
        /// <summary>
        /// The value of the note property.
        /// </summary>
        public object? Value { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PSNotePropertyDefinition);
        }

        /// <inheritdoc/>
        public bool Equals(PSNotePropertyDefinition? other)
        {
            return other != null &&
                   TypeName == other.TypeName &&
                   MemberName == other.MemberName &&
                   EqualityComparer<object?>.Default.Equals(Value, other.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -748927776;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MemberName);
            hashCode = hashCode * -1521134295 + (Value is null ? 0 : EqualityComparer<object?>.Default.GetHashCode(Value));
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(PSNotePropertyDefinition? left, PSNotePropertyDefinition? right)
        {
            return EqualityComparer<PSNotePropertyDefinition?>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(PSNotePropertyDefinition? left, PSNotePropertyDefinition? right)
        {
            return !(left == right);
        }
        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        public PSNotePropertyDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> memberName = default,
            DefaultOrUpdate<object?> value = default
            ) => new PSNotePropertyDefinition(
                typeName.GetValueOrDefault(TypeName),
                memberName.GetValueOrDefault(MemberName),
                value.GetValueOrDefault(Value)
                );
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>{TypeName}.{MemberName} = {Value}</returns>
        public override string ToString() => $"{TypeName}.{MemberName} = {Value}";
    }
}