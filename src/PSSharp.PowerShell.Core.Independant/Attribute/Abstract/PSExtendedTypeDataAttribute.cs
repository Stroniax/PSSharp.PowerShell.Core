using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using PSSharp.ExtendedTypeDataAnalysis;

namespace PSSharp
{
    /// <summary>
    /// Base class for all attributes that define PowerShell type data.
    /// </summary>
    public abstract class PSExtendedTypeDataAttribute : Attribute
    {
        /// <summary>
        /// Gets a definition object for the type data represented by this attribute based on the instance that
        /// this attribute is applied to.
        /// </summary>
        /// <param name="attributeTarget"></param>
        /// <returns></returns>
        abstract internal IEnumerable<PSExtendedTypeDataDefinition> GetDefinitions(ICustomAttributeProvider attributeTarget);
        /// <summary>
        /// Default constructor.
        /// </summary>
        internal PSExtendedTypeDataAttribute()
        {

        }
    }
}