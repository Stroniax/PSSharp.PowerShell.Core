namespace PSSharp
open System
open System.Reflection
open System.Management.Automation

/// <summary>
/// Transforms any generic <see cref="T:Async{T}"/> item into an instance of <see cref="T:Async{obj}"/>
/// that can be accepted by a cmdlet parameter while still strongly typing the parameter as as async computation.
/// If the return type of the computation was changed, the PSObject representation will contain a
/// member named "OriginalComputation" with the unboxed computation.
/// </summary>
type FSharpAsyncObjTransformationAttribute () =
    inherit EnumeratedArgumentTransformationAttribute ()

    static let boxAsyncReturnValue v = async { return box v }
    static let bindFunctionReflected = typeof<FSharpAsyncObjTransformationAttribute>.GetMethod(
        nameof boxAsyncReturnValue,
        BindingFlags.NonPublic ||| BindingFlags.Static
        )

    override _.TransformElement(_, item) =
        // I need to identify the current return type of the computation, and quit if "item" is not Async<_>
        if item = null then NotTransformed else
        let itemType = item.GetType()
        if not itemType.IsGenericType then NotTransformed else
        let genericItemType = itemType.GetGenericTypeDefinition()
        if genericItemType <> typedefof<Async<_>> then NotTransformed else
        let returnType = itemType.GetGenericArguments()[0]
        if returnType = typeof<obj> then NotTransformed else
        let boxedAsync = bindFunctionReflected.MakeGenericMethod(itemType).Invoke(null, [|item|])
        let pso = boxedAsync |> pso
        pso.Properties.Add(new PSNoteProperty("OriginalComputation", item))
        pso.Properties.Add(new PSNoteProperty("ReturnType", returnType))
        TransformationResult.Single boxedAsync
        