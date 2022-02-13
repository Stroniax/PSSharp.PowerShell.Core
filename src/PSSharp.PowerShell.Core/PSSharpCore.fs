namespace PSSharp

/// PSObject conversion and aliasing.
[<AutoOpen>]
[<CompiledName("PSObjectModule")>]
module PSObject =
    open System.Management.Automation
    open System.Management.Automation.Internal
    open System.Diagnostics.CodeAnalysis
    type pso = PSObject
    
    [<CompiledName("PSAutomationNull")>]
    let psnull = AutomationNull.Value
    
    [<CompiledName("AsPSObject")>]
    [<return: NotNullIfNotNullAttribute("obj")>]
    let inline pso ([<AllowNull>] obj: obj) =
        match obj with
        | null -> null
        | _ -> PSObject.AsPSObject(obj)

    [<CompiledName("GetPSObjectBase")>]
    [<return: NotNullIfNotNullAttribute("obj")>]
    let psbase ([<AllowNull>] obj: obj) =
        match obj with
        | :? PSObject as pso -> pso.BaseObject
        | _ -> obj

    [<CompiledName("PSCast")>]
    let pscast v = LanguagePrimitives.ConvertTo(v)
    [<CompiledName("PSTryCast")>]
    let psTryCast v =
        match LanguagePrimitives.TryConvertTo(v) with
        | true, r -> Some r
        | false, _ -> None

/// PSSharp.Core helpers
[<AutoOpen>]
module internal PSSharpCore =
    open System
    open System.Runtime.CompilerServices
    
    [<assembly: Extension>]
    do()

    type ErrorMessages = PSSharp.Errors
    
    type Async with
        static member AwaitValueTask (valueTask : System.Threading.Tasks.ValueTask) =
            if valueTask.IsCompleted then
                async { () }
            else
                valueTask.AsTask() |> Async.AwaitTask
        static member AwaitValueTask (valueTask : System.Threading.Tasks.ValueTask<'T>) =
            if valueTask.IsCompleted then
                async { return valueTask.Result }
            else
                valueTask.AsTask() |> Async.AwaitTask

module ``CSharpNullable`` =
    type internal NullableAttribute = System.Runtime.CompilerServices.NullableAttribute

    [<Literal>]
    let internal NullableAttributeValue = 2uy

    [<Literal>]
    let internal NonNullableAttributeValue = 1uy

