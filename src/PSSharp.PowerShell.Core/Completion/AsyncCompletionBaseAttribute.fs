namespace PSSharp
open System
open System.Management.Automation
open System.Management.Automation.Language
open System.Collections
open System.Collections.Generic
open System.Threading

/// Base class for async argument completer that produces results as IAsyncEnumerable.
[<AbstractClass>]
type AsyncCompletionBaseAttribute () =
    inherit CompletionBaseAttribute ()

    static let onConsoleCancelKeyPress (cts: CancellationTokenSource) (sender: obj) (args: ConsoleCancelEventArgs) =
        cts.Cancel()

    abstract CompleteArgumentAsync:
        commandName: string *
        parameterName: string *
        wordToComplete: string *
        commandAst: CommandAst *
        fakeBoundParameters: IDictionary *
        [<System.Runtime.CompilerServices.EnumeratorCancellation>] cancellationToken: CancellationToken
            -> IAsyncEnumerable<CompletionResult>

    override this.CompleteArgument(commandName, parameterName, wordToComplete, commandAst, fakeBoundParameters) =
        use cts = new CancellationTokenSource ()
        let handler = new ConsoleCancelEventHandler(onConsoleCancelKeyPress cts)
        Console.CancelKeyPress.AddHandler handler
        try
            let enumerable = this.CompleteArgumentAsync(
                commandName,
                parameterName,
                wordToComplete,
                commandAst,
                fakeBoundParameters,
                cts.Token
                )
            let enumerator = enumerable.GetAsyncEnumerator(cts.Token)
            let values = new List<CompletionResult>()
            let rec tryGetNext () =
                try
                    match enumerator.MoveNextAsync() |> Async.AwaitTaskLike |> Async.RunSynchronously with
                    | true ->
                        values.Add enumerator.Current
                        tryGetNext ()
                    | false -> ()
                with _ -> ()
            try
                tryGetNext()
                values
            finally
                enumerator.DisposeAsync() |> Async.AwaitTaskLike |> Async.RunSynchronously
        finally
            Console.CancelKeyPress.RemoveHandler handler
