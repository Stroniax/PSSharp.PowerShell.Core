namespace PSSharp
open System
open System.Management.Automation

/// Transforms a type name from a string or PSTypeName instance into the corresponding Type instance.
type StringToTypeTransformationAttribute () =
    inherit EnumeratedArgumentTransformationAttribute ()

    override _.TransformElement(_, element) =    
        match element with
        | :? string as typeName ->
            let typeName = 
                if typeName[0] = '[' && typeName[typeName.Length - 1] = ']' then typeName.Substring(1, typeName.Length - 2)
                else typeName
            match PSLanguagePrimitives.tryCast typeName with
            | ValueSome (t: Type) when t <> null -> TransformationResult.Single t
            | _ -> NotTransformed
        | :? PSTypeName as typeName ->
            match typeName.Type with
            | null -> NotTransformed
            | t -> TransformationResult.Single t
        | _ -> NotTransformed