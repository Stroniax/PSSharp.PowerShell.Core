using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Identifies a PowerShell script property.
    /// </summary>
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSTypeTarget, AllowMultiple = true, Inherited = false)]
    public sealed class PSScriptPropertyAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiates an attribute defining a PowerShell script property for the type
        /// this attribute is applied to.
        /// </summary>
        /// <param name="name">The name of the script property.</param>
        /// <param name="getScriptText">The getter script for the property. May not be <see langword="null"/>
        /// if <paramref name="setScriptText"/> is <see langword="null"/>.</param>
        /// <param name="setScriptText">The setter script for the property. May not be <see langword="null"/>
        /// if <paramref name="getScriptText"/> is <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public PSScriptPropertyAttribute(string name, string? getScriptText, string? setScriptText)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            GetScriptText = getScriptText;
            SetScriptText = setScriptText;
            if ((getScriptText ?? setScriptText) is null)
            {
                throw new InvalidOperationException(Errors.GetOrSetRequired);
            }
        }
        /// <inheritdoc cref="PSScriptPropertyAttribute(string, string?, string?)"/>
        public PSScriptPropertyAttribute(string name, string getScriptText)
            : this(name, getScriptText ?? throw new ArgumentNullException(nameof(getScriptText)), null)
        {
        }
        /// <summary>
        /// The name of the script property.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The getter script for the property.
        /// </summary>
        public string? GetScriptText { get; }
        /// <summary>
        /// The setter script for the property.
        /// </summary>
        public string? SetScriptText { get; }
        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToType = (Type)attributeTarget;
            return new PSExtendedTypeDataDefinition[]
            {
                new PSScriptPropertyDefinition(
                    appliedToType.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired),
                    Name,
                    GetScriptText,
                    SetScriptText
                    )
            };
        }
    }
}
