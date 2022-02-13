namespace PSSharp
open System.Management.Automation
open System.Management.Automation.Language
open System.Collections
open System.Collections.Generic
open System.Threading.Tasks
open System.Runtime.CompilerServices

/// Base class for async argument completer using the Async<_> computation expression type.
[<AbstractClass>]
type AsyncComputationCompletionBaseAttribute () =
    inherit AsyncCompletionBaseAttribute ()
    override this.CompleteArgumentAsync(commandName, parameterName, wordToComplete, commandAst, fakeBoundParameters, cancellationToken) =
        {
            new IAsyncEnumerable<CompletionResult> with
                member _.GetAsyncEnumerator ([<EnumeratorCancellation>] cancellationToken) =
                    let mutable current = ValueNone
                    let mutable state = this.GetInitialState()
                    {
                        new IAsyncEnumerator<CompletionResult> with
                            member _.DisposeAsync () = ValueTask.CompletedTask
                            member _.MoveNextAsync () =
                                let moveNextComputation =
                                    async {
                                        let! next = this.GetNextCompletionAsync(commandName, parameterName, wordToComplete, commandAst, fakeBoundParameters, &state)
                                        current <- next
                                        return current.IsSome
                                    }
                                let moveNextTask = Async.StartAsTask(moveNextComputation, cancellationToken = cancellationToken)
                                new ValueTask<bool>(moveNextTask)
                            member _.Current =
                                match current with
                                | ValueSome v -> v
                                | ValueNone -> invalidOp "Attempt to get Current from invalid position in the enumerator."
                    }
        }

    abstract GetNextCompletionAsync :
        commandName: string *
        parameterName : string *
        wordToComplete : string *
        commandAst : CommandAst *
        fakeBoundParameters : IDictionary *
        state: obj byref
            -> Async<ValueOption<CompletionResult>>

    abstract GetInitialState : unit -> obj