namespace PSSharp
open System

/// Transformation attribute to convert an object into the type of the object.
/// If the input is a type name, the type with the provided name will be returned.
type ObjectToTypeTransformationAttribute () =
    inherit FlatteningTransformationAttribute<Type>()

    override _.TransformArgument(engineIntrinsics, inputData) =
        match inputData with
        | null -> TransformationResult.Single null
        | :? Type -> TransformationResult.NotTransformed()
        | :? string as typeName ->
            match PSLanguagePrimitives.tryCast typeName with
            | ValueSome t -> TransformationResult.Single t
            | ValueNone -> TransformationResult.Single (inputData.GetType())
        | _ -> TransformationResult.Single (inputData.GetType())
