namespace PSSharp
open System
open System.Management.Automation
open System.Collections.Generic
open System.Linq

/// Use a script to expand wildcard patterns provided to a parameter.
/// If the script returns no values, the wildcarded pattern is passed through.
type ScriptWildcardExpansionAttribute (script : ScriptBlock, expandNonWildcardStrings) =
    inherit WildcardExpansionAttribute(expandNonWildcardStrings)


    new(script : ScriptBlock) = ScriptWildcardExpansionAttribute(script, false)
    /// Create a script transformation from script text. Useful for compiled languages where
    /// the ScriptBlock type is not allowed in an attribute.
    new(scriptText) = ScriptWildcardExpansionAttribute(ScriptBlock.Create(scriptText))
    /// Create a script transformation from script text. Useful for compiled languages where
    /// the ScriptBlock type is not allowed in an attribute.
    new(scriptText, expandNonWildcardStrings) = ScriptWildcardExpansionAttribute(ScriptBlock.Create(scriptText), expandNonWildcardStrings)

    override _.Expand(engineIntrinsics, pattern) =
        let variables = new List<PSVariable>(1)
        variables.Add(new PSVariable(SpecialVariables.psItem, pattern))
        let expandedValues = script.InvokeWithContext(null, variables, pattern, engineIntrinsics)
        match expandedValues.Count with
        | 0 -> NotTransformed
        | _ -> Collection [ for v in expandedValues do box v ]