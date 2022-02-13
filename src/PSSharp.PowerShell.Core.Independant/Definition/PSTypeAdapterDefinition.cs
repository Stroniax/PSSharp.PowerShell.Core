using System;
using System.Collections.Generic;

namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell TypeAdapter type data extension definition.
    /// </summary>
    [PSScriptProperty("TypeAdapterType", "$this.TypeAdapterTypeName -as [Type]")]
    public sealed class PSTypeAdapterDefinition : PSExtendedTypeDataDefinition, IEquatable<PSTypeAdapterDefinition?>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="typeAdapterTypeName"></param>
        public PSTypeAdapterDefinition(string typeName, string typeAdapterTypeName)
            : base(typeName)
        {
            TypeAdapterTypeName = typeAdapterTypeName;
        }
        /// <summary>
        /// The name of the type that is used to get and use members for instances of the type represented by <see cref="PSExtendedTypeDataDefinition.TypeName"/>.
        /// </summary>
        public string TypeAdapterTypeName { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PSTypeAdapterDefinition);
        }

        /// <inheritdoc/>
        public bool Equals(PSTypeAdapterDefinition? other)
        {
            return other != null &&
                   TypeName == other.TypeName &&
                   TypeAdapterTypeName == other.TypeAdapterTypeName;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -131001859;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeAdapterTypeName);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(PSTypeAdapterDefinition? left, PSTypeAdapterDefinition? right)
        {
            return EqualityComparer<PSTypeAdapterDefinition?>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(PSTypeAdapterDefinition? left, PSTypeAdapterDefinition? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        public PSTypeAdapterDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> typeAdapterTypeName = default
            ) => new PSTypeAdapterDefinition(
                typeName.GetValueOrDefault(TypeName),
                typeAdapterTypeName.GetValueOrDefault(TypeAdapterTypeName)
                );

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>Type Converter [{TypeConverterTypeName}] for [{TypeName}]</returns>
        public override string ToString() => $"Type Adapter [{TypeAdapterTypeName}] for [{TypeName}]";
    }
}
