namespace PSSharp
open System
open System.Management.Automation
open System.Management.Automation.Language

/// Transformation attribute to expand a wildcard pattern into available outcomes.
[<AbstractClass>]
type WildcardExpansionAttribute (expandNonWildcardStrings) =
    inherit EnumeratedArgumentTransformationAttribute ()

    new() = WildcardExpansionAttribute(false)

    override this.TransformElement(engineIntrinsics: EngineIntrinsics, element: obj) =
        match element with
        | :? string as str ->
            if expandNonWildcardStrings || WildcardPattern.ContainsWildcardCharacters(str) then
                this.Expand(engineIntrinsics, str)
                |> TransformationResult.box TransformationResult.NotTransformed
            else
                NotTransformed
        | _ -> NotTransformed

    abstract Expand :
        engineIntrinsics : EngineIntrinsics *
        pattern : string
            -> TransformationResult<obj>