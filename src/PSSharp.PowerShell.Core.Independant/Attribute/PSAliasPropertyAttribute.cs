using PSSharp.ExtendedTypeDataAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Indicates that the property this attribute is applied to should also be referenced
    /// by the alias indicated by <see cref="AliasName"/>.
    /// </summary>
    [AttributeUsage(PSExtendedTypeDataAnalysis.PSPropertyTarget, AllowMultiple = true, Inherited = false)]
    public sealed class PSAliasPropertyAttribute : PSExtendedTypeDataAttribute
    {
        /// <summary>
        /// Instantiate an attribute indicating an alias property that references the property or field on which
        /// this attribute is defined.
        /// </summary>
        /// <param name="aliasName"><inheritdoc cref="AliasName" path="/summary"/></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PSAliasPropertyAttribute(string aliasName)
        {
            AliasName = aliasName ?? throw new ArgumentNullException(nameof(aliasName));
        }
        /// <summary>
        /// The name of the alias property.
        /// </summary>
        public string AliasName { get; }
        internal override IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget)
        {
            var appliedToMember = (MemberInfo)attributeTarget;
            return new[]
            {
                new PSAliasPropertyDefinition(
                    appliedToMember.DeclaringType?.FullName ?? throw new InvalidOperationException(Errors.TypeFullNameRequired),
                    AliasName,
                    appliedToMember.Name
                    )
            };
        }
    }
}
