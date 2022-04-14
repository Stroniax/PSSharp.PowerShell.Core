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
    open System.Threading
    open System.Threading.Tasks
    open System.Collections.Generic
    
    [<assembly: Extension>]
    do()

    type ErrorMessages = PSSharp.Errors
    
    type Async with
        /// Creates a computation that proceeds when any C# Task-Like awaitable object (including Task,
        /// ValueTask, YieldAwaitable, and others).
        static member inline AwaitTaskLike awaitable =
            Async.FromContinuations( fun (cont, econt, ccont) ->
                let awaiter = (^a : (member GetAwaiter : unit -> ^b) awaitable)
                let continuation () =
                    let res = try Ok (^b : (member GetResult: unit -> 'c) awaiter)
                              with e -> Error e
                    match res with
                    | Ok result -> cont result
                    | Error exn -> econt exn
                match (^b : (member IsCompleted: bool) awaiter) with
                | true -> continuation()
                | false -> (^b : (member OnCompleted: Action -> unit) (awaiter, continuation))
            )
    type AsyncBuilder with
        /// Expand the AsyncBuilder to call TryFinally with an async finally block.
        member this.TryFinally (computation: Async<'T>, compensation: unit -> Async<unit>) =
            async {
                let! res =
                    async {
                        try
                            let! s = computation
                            return Ok s
                        with e -> return Error e
                    }
                do! compensation()
                match res with
                | Ok v -> return v
                | Error e -> return raise e
            }
        /// Expand the AsyncBuilder to call Using with an IAsyncDisposable instance, using
        /// DisposeAsync to clean up resources.
        member this.Using (resource : 'T when 'T :> IAsyncDisposable, binder: 'T -> Async<'U>) : Async<'U> =
            let mutable x = 0
            let disposeFunction () =
                if Interlocked.CompareExchange(&x, 1, 0) = 0 then this.Delay(resource.DisposeAsync >> Async.AwaitTaskLike)
                else this.Zero()
            this.TryFinally((binder resource), disposeFunction)
        /// Expand the AsyncBuilder to run a while loop with an async predicate expression,
        /// e.g. `while async { true } do`.
        member this.While (guard: unit -> Async<bool>, computation: Async<unit>) =
            this.Bind(
                guard(),
                fun p ->
                    if p then
                        this.Combine(
                            computation,
                            this.While(
                                guard,
                                computation
                                )
                            )
                    else
                        this.Zero()
                    )
        /// Expand the AsyncBuilder to enumerate contents of an IAsyncEnumerable instance in a for loop.
        member this.For (aseq: #IAsyncEnumerable<'T>, body: 'T -> Async<unit>) =
            this.Bind(Async.CancellationToken, fun ct ->
                this.Using(aseq.GetAsyncEnumerator(ct), fun enumerator ->
                    this.While(enumerator.MoveNextAsync >> Async.AwaitTaskLike, this.Delay(fun () -> body enumerator.Current))
                    )
                )

module ``CSharpNullable`` =
    type internal NullableAttribute = System.Runtime.CompilerServices.NullableAttribute

    [<Literal>]
    let internal NullableAttributeValue = 2uy

    [<Literal>]
    let internal NonNullableAttributeValue = 1uy