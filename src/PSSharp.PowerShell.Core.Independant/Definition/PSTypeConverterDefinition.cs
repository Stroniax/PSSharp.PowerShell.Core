using System;
using System.Collections.Generic;
using System.Text;

namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// Represents a PowerShell TypeConverter type data extension definition.
    /// </summary>
    [PSScriptProperty("TypeConverterType", "$this.TypeConverterTypeName -as [Type]")]
    public sealed class PSTypeConverterDefinition : PSExtendedTypeDataDefinition, IEquatable<PSTypeConverterDefinition?>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="typeConverterTypeName"></param>
        public PSTypeConverterDefinition(string typeName, string typeConverterTypeName)
            : base(typeName)
        {
            TypeConverterTypeName = typeConverterTypeName ?? throw new ArgumentNullException(nameof(typeConverterTypeName));
        }
        /// <summary>
        /// The name of the type that is used to convert to and from instances of the type represented by <see cref="PSExtendedTypeDataDefinition.TypeName"/>.
        /// </summary>
        public string TypeConverterTypeName { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as PSTypeConverterDefinition);
        }

        /// <inheritdoc/>
        public bool Equals(PSTypeConverterDefinition? other)
        {
            return other != null &&
                   TypeName == other.TypeName &&
                   TypeConverterTypeName == other.TypeConverterTypeName;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -1797805830;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeConverterTypeName);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(PSTypeConverterDefinition? left, PSTypeConverterDefinition? right)
        {
            return EqualityComparer<PSTypeConverterDefinition?>.Default.Equals(left, right);
        }
        /// <inheritdoc/>
        public static bool operator !=(PSTypeConverterDefinition? left, PSTypeConverterDefinition? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        public PSTypeConverterDefinition CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> typeConverterTypeName = default
            ) => new PSTypeConverterDefinition(
                typeName.GetValueOrDefault(TypeName),
                typeConverterTypeName.GetValueOrDefault(TypeConverterTypeName)
                );

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>Type Converter [{TypeConverterTypeName}] for [{TypeName}]</returns>
        public override string ToString() => $"Type Converter [{TypeConverterTypeName}] for [{TypeName}]";
    }
}
