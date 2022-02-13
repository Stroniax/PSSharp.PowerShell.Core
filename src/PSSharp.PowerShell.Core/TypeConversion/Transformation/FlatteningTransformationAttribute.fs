namespace PSSharp
open System
open System.Collections
open System.Collections.Generic
open System.Management.Automation

[<AbstractClass>]
type FlatteningTransformationAttribute<'a> () =
    inherit ArgumentTransformationAttribute ()

    /// Transform a value into one or more result values. The values will be flattened
    /// before passed to the parameter.
    abstract member TransformArgument : engineIntrinsics : EngineIntrinsics * inputData : obj -> TransformationResult<'a>

    override this.Transform(engineIntrinsics, inputData) =
        this.TransformArgument(engineIntrinsics, inputData)
        |> TransformationResult.unwrap (fun () -> inputData)

