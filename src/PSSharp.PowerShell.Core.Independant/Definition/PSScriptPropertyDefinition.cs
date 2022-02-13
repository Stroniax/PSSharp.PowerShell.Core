using System;
using System.Collections.Generic;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell ScriptProperty definition.
    /// </summary>
    [PSScriptProperty("GetScript", "[ScriptBlock]::Create($this.GetScriptText)")]
    [PSScriptProperty("SetScript", "[ScriptBlock]::Create($this.SetScriptText)")]
    public sealed class PSScriptPropertyDefinition
        : PSExtendedTypeDataMemberDefinition, IEquatable<PSScriptPropertyDefinition?>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="memberName"></param>
        /// <param name="getScriptText"></param>
        /// <param name="setScriptText"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public PSScriptPropertyDefinition(
            string typeName,
            string memberName,
            string? getScriptText,
            string? setScriptText
            )
            : base(typeName, memberName)
        {
            GetScriptText = getScriptText;
            SetScriptText = setScriptText;
            if ((getScriptText ?? setScriptText) is null)
            {
                throw new InvalidOperationException(Errors.GetOrSetRequired);
            }
        }
        /// <summary>
        /// The text representation of the Get script.
        /// </summary>
        public string? GetScriptText { get; }
        /// <summary>
        /// The text representation of the Set script.
        /// </summary>
        public string? SetScriptText { get; }
        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PSScriptPropertyDefinition);
        }

        /// <inheritdoc/>
        public bool Equals(PSScriptPropertyDefinition? other)
        {
            return other != null &&
                   TypeName == other.TypeName &&
                   MemberName == other.MemberName &&
                   GetScriptText == other.GetScriptText &&
                   SetScriptText == other.SetScriptText;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = 1719207352;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MemberName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(GetScriptText ?? string.Empty);
            hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(SetScriptText ?? string.Empty);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(PSScriptPropertyDefinition? left, PSScriptPropertyDefinition? right)
        {
            return EqualityComparer<PSScriptPropertyDefinition?>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(PSScriptPropertyDefinition? left, PSScriptPropertyDefinition? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        /// <returns></returns>
        public PSScriptPropertyDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> memberName = default,
            DefaultOrUpdate<string?> getScriptText = default,
            DefaultOrUpdate<string?> setScriptText = default
            ) =>
            new PSScriptPropertyDefinition(
                typeName.GetValueOrDefault(TypeName),
                memberName.GetValueOrDefault(MemberName),
                getScriptText.GetValueOrDefault(GetScriptText),
                setScriptText.GetValueOrDefault(SetScriptText)
                );
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>{TypeName}.{MemberName} with {get|set|get and set}</returns>
        public override string ToString()
        {
            var gettable = GetScriptText is null ? null : " with get";
            var settable = SetScriptText is null
                ? gettable is null
                ? " with set"
                : " and set"
                : null;
            return $"{TypeName}.{MemberName}{gettable}{settable}";
        }
    }
}