namespace PSSharp
open System
open System.Management.Automation

/// Transformation attribute for two strongly-typed values.
[<AbstractClass>]
type StronglyTypedTransformationAttribute<'TInput, 'TOutput> () =
    inherit EnumeratedArgumentTransformationAttribute ()

    override this.TransformElement(engineIntrinsics : EngineIntrinsics, element : obj) =
        match element with
        | :? 'TInput as ofInput ->
            this.TryTransformElement(engineIntrinsics, ofInput)
            |> TransformationResult.box TransformationResult.NotTransformed
        | _ -> TransformationResult.notTransformed

    abstract TryTransformElement : engineIntrinsics : EngineIntrinsics * element : 'TInput -> TransformationResult<'TOutput>