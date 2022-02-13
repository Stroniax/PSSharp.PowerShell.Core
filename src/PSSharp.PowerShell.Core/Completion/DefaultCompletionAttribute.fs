namespace PSSharp
open System

/// Indicate that the default argument completion is acceptable. This attribute is used in
/// tests and code analysis to supress warnings for missing completion, and has no actual
/// effect at runtime.
[<AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)>]
type DefaultCompletionAttribute () =
    inherit Attribute()