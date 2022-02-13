using System;
using System.Collections.Generic;
namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell CodeProperty definition.
    /// </summary>
    public sealed class PSCodePropertyDefinition
        : PSExtendedTypeDataMemberDefinition,
        IEquatable<PSCodePropertyDefinition?>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"><inheritdoc cref="TypeName" path="/summary"/></param>
        /// <param name="memberName"><inheritdoc cref="MemberName" path="/summary"/></param>
        /// <param name="get"><inheritdoc cref="GetCodeReference" path="/summary"/></param>
        /// <param name="set"><inheritdoc cref="SetCodeReference" path="/summary"/></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSCodePropertyDefinition(
            string typeName,
            string memberName,
            CodeReference? get,
            CodeReference? set
            )
            : base(typeName, memberName)
        {
            if ((get ?? set) is null)
            {
                throw new InvalidOperationException(Errors.GetOrSetRequired);
            }
            GetCodeReference = get;
            SetCodeReference = set;
        }
        /// <summary>
        /// A reference to the method used as a getter for this property.
        /// </summary>
        public CodeReference? GetCodeReference { get; }
        /// <summary>
        /// A reference to the method used as a setter for this property.
        /// </summary>
        public CodeReference? SetCodeReference { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as PSCodePropertyDefinition);

        /// <inheritdoc/>
        public bool Equals(PSCodePropertyDefinition? other)
            => other != null
            && TypeName == other.TypeName
            && MemberName == other.MemberName
            && EqualityComparer<CodeReference?>.Default.Equals(GetCodeReference, other.GetCodeReference)
            && EqualityComparer<CodeReference?>.Default.Equals(SetCodeReference, other.SetCodeReference);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -1130192432;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MemberName);
            hashCode = hashCode * -1521134295 + (GetCodeReference is null ? 0 : EqualityComparer<CodeReference?>.Default.GetHashCode(GetCodeReference));
            hashCode = hashCode * -1521134295 + (SetCodeReference is null ? 0 : EqualityComparer<CodeReference?>.Default.GetHashCode(SetCodeReference));
            return hashCode;
        }


        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        public PSCodePropertyDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> memberName = default,
            DefaultOrUpdate<CodeReference?> get = default,
            DefaultOrUpdate<CodeReference?> set = default
            )
        {
            return new PSCodePropertyDefinition(
                typeName.GetValueOrDefault(TypeName),
                memberName.GetValueOrDefault(MemberName),
                get.GetValueOrDefault(GetCodeReference),
                set.GetValueOrDefault(SetCodeReference)
                );
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>{TypeName}.{MemberName} with {get|set|get and set}</returns>
        public override string ToString()
        {
            var gettable = GetCodeReference is null ? null : " with get";
            var settable = SetCodeReference is null
                ? gettable is null
                ? " with set"
                : " and set"
                : null;
            return $"{TypeName}.{MemberName}{gettable}{settable}";
        }
    }
}