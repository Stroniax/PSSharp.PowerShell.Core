using System;
using System.ComponentModel;

namespace PSSharp.ExtendedTypeDataAnalysis
{
    /// <summary>
    /// A reference to a type and method that can be used as a PowerShell CodeProperty or CodeMethod.
    /// At runtime, this must refer to a compiled MethodInfo. The method must accept a first parameter
    /// of type PSObject.
    /// <para>If this method is used as a property setter, the method must accept a second parameter of
    /// the same type as the property getter method returns.</para>
    /// <para>If this method is used as a code method, additional parameters become parameters to the
    /// method on the PSObject.</para>
    /// <para>If this method is used as a property getter, the method may have no additional parameters.</para>
    /// </summary>
    [ReadOnly(true)]
    [PSScriptProperty("Type", "$this.TypeName -as [Type]")]
    [PSScriptMethod("GetMethodInfo", "if ($this.Type) { $this.Type.GetMethods([System.Reflection.BindingFlags]'Public,Static').Where{$_.Name -eq $this.MethodName -and $_.GetParameters()[0].ParameterType -eq [psobject]}[0] }")]
    public sealed class CodeReference : IEquatable<CodeReference>
    {
        /// <summary>
        /// The name of the type that defines the method called by this code reference.
        /// </summary>
        public string TypeName { get; }
        /// <summary>
        /// The name of the method that is invoked when calling the code reference.
        /// </summary>
        public string MethodName { get; }
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="typeName"><inheritdoc cref="TypeName" path="/summary"/></param>
        /// <param name="methodName"><inheritdoc cref="MethodName" path="/summary"/></param>
        /// <exception cref="ArgumentNullException"></exception>
        public CodeReference(string typeName, string methodName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        }

        /// <inheritdoc/>
        public bool Equals(CodeReference? other) =>
            other != null
            && other.TypeName == TypeName
            && other.MethodName == MethodName;
        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as CodeReference);
        public override int GetHashCode() => $"[{TypeName}]::{MethodName}".GetHashCode();
        /// <summary><inheritdoc/></summary>
        /// <returns>[{TypeName}]::{MethodName}</returns>
        public override string ToString() => $"[{TypeName}]::{MethodName}";

        /// <summary>
        /// Creates a shallow copy of this immutable resource with copy-and-update functionality.
        /// </summary>
        public CodeReference CopyAndUpdate(
            DefaultOrUpdate<string> typeName = default,
            DefaultOrUpdate<string> methodName = default
            )
            => new CodeReference(typeName.GetValueOrDefault(TypeName), methodName.GetValueOrDefault(MethodName));
    }
}