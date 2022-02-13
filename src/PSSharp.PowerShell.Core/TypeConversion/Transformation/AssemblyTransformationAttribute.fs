namespace PSSharp
open System
open System.Reflection
open System.Management.Automation
open System.Linq

type AssemblyTransformationAttribute () =
    inherit EnumeratedArgumentTransformationAttribute()

    static member private TryTransformFromName(name : string) =
        try ValueSome <| Assembly.Load(name)
        with _ -> ValueNone
    static member private TryTransformFromPath(path: string) =
        try ValueSome <| Assembly.LoadFrom(path)
        with _ -> ValueNone
    static member private TryTransform(pathOrName : string) : Assembly voption =
        AssemblyTransformationAttribute.TryTransformFromPath(pathOrName)
        |> ValueOption.orElseWith (fun () -> AssemblyTransformationAttribute.TryTransformFromName(pathOrName))

    override _.TransformElement(engineIntrinsics, element) =
        match element with
        | null -> Collection(Array.empty)
        | PSLanguagePrimitives.PSAsType (assembly : Assembly) -> TransformationResult.Single(assembly)
        | PSLanguagePrimitives.PSAsType (assemblies : Assembly[]) ->
            let boxedAssemblyArray : obj[] = PSLanguagePrimitives.cast assemblies
            TransformationResult.FlattenableCollection(boxedAssemblyArray)
        | :? string as pathOrName ->
            match AssemblyTransformationAttribute.TryTransform pathOrName with
            | ValueSome assembly -> TransformationResult.Single(assembly)
            | ValueNone -> TransformationResult.Collection(Array.empty)
        | _ -> TransformationResult.notTransformed
