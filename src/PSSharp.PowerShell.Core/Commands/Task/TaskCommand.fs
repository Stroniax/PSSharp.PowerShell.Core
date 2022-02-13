namespace PSSharp.Commands
open System
open System.Threading
open System.Threading.Tasks
open System.Management.Automation
open PSSharp
open TaskLike

/// <summary>
/// Base command for working with <see cref="Task"/> instances.
/// </summary>
type TaskCmdlet () =
    inherit Cmdlet ()
    let cts = new CancellationTokenSource ()

    /// Token that is cancelled when the cmdlet is stopped, such as when the user aborts the cmdlet
    /// by pressing Ctrl+C at the console.
    member _.StoppingToken = cts.Token

    /// Waits for a task to complete (allowing cancellation if the cmdlet is terminated).
    /// Returns the result of the task.
    member _.WaitTask (task: Task<_>) =
        let async = task |> Async.AwaitTask
        Async.RunSynchronously(async, cancellationToken = cts.Token)
    /// Waits for a  task to complete (allowing cancellation if the cmdlet is terminated).
    /// Returns a ValueOption with the task result if it is Task<_> at runtime: otherwise
    /// returns ValueNone.
    member _.WaitTask (task: Task) =
        let async = task |> awaitTaskOrTaskOf
        Async.RunSynchronously(async, cancellationToken = cts.Token)
    /// Safely waits for a task to complete and provides the result.
    member this.SafeWaitTask (task : Task<_>) =
        try this.WaitTask task |> Ok
        with | e -> Result.Error e
    /// <summary>
    /// Safely waits for a task to complete and provides the result if it is determined to be
    /// <see cref="Task{TResult}"/> at runtime.
    /// </summary>
    member this.SafeWaitTask (task : Task) =
        try this.WaitTask task |> Ok
        with | e -> Result.Error e

    /// <summary>
    /// Cancel the <see cref="StoppingToken"/>.
    /// </summary>
    override _.StopProcessing () =
        cts.Cancel()
        base.StopProcessing()

    /// Cleanup cancellation related members.
    member _.Dispose () =
        cts.Dispose ()

    interface IDisposable with
        member this.Dispose () =
            this.Dispose()