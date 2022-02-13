namespace PSSharp
open System
open System.Runtime.Serialization
open System.Management.Automation
open System.Threading
open System.Threading.Tasks

/// An operation to be executed by a DelegateJob.
type DelegateJobOperation = delegate of write: (PSInformationalItem -> unit) -> Async<unit>

/// An exception to raise in a DelegateJob that will stop the job without writing the error to the
/// job's error stream. Use this when the appropriate error was already reported.
[<NoEquality; NoComparison>]
type DelegateJobStopException =
    inherit Exception
    new() = { inherit Exception() }
    new(message: string) = { inherit Exception(message) }
    new(message: string, innerException: exn) = { inherit Exception(message, innerException) }
    new(serializationInfo : SerializationInfo, streamingContext : StreamingContext) =
        { inherit Exception(serializationInfo, streamingContext) }

/// PowerShell job representing a delegated asynchronous operation.
[<NoEquality;NoComparison>]
type DelegateJob (operation: DelegateJobOperation, command, name) =
    inherit JobBase(command, name)

    /// Cancellation for the operation, when started.
    let cts = new CancellationTokenSource ()
    
    /// Convert the common C# input for this job into what we'll work with in F#.
    static let ofTaskFunc (op: Func<PSInformationalItemWriter, Task>) =
        new DelegateJobOperation(
            fun write ->
                async {
                    do! Async.AwaitTask (op.Invoke(write))
                })


    new(operation: DelegateJobOperation) = new DelegateJob(operation, null, null)
    new(operation: DelegateJobOperation, command) = new DelegateJob(operation, command, null)
    new(operation : Func<PSInformationalItemWriter, Task>) = new DelegateJob(operation |> ofTaskFunc)
    new(operation : Func<PSInformationalItemWriter, Task>, command) = new DelegateJob(operation |> ofTaskFunc, command)
    new(operation : Func<PSInformationalItemWriter, Task>, command, name) = new DelegateJob(operation |> ofTaskFunc, command, name)

    member private this.FailWithException(exn: exn) =
        match exn with
        | :? DelegateJobStopException -> ()
        | _ ->
            let er = new ErrorRecord(
                exn,
                exn.GetType().Name,
                ErrorCategory.NotSpecified,
                null
                )
            this.Error.Add er
        this.SetJobState(JobState.Failed)
    member private this.OnCancelled(_) =
        this.SetJobState(JobState.Stopped)
    member private this.OnCompleted() =
        this.SetJobState(JobState.Completed)

    member this.StartJob () =
        match this.JobStateInfo.State with
        | JobState.NotStarted ->
            let write item =
                match item with
                | Undefined -> ()
                | Output o -> this.Output.Add o
                | PSInformationalItem.Error e -> this.Error.Add e
                | Warning w -> this.Warning.Add w
                | Verbose v -> this.Verbose.Add v
                | Debug d -> this.Debug.Add d
                | Information i -> this.Information.Add i
                | Progress p -> this.Progress.Add p

            let asyncWithCatch = async {
                try
                    do! operation.Invoke(write)
                    this.OnCompleted()
                with e -> this.FailWithException e
            }
            let asyncWithCanceleld = Async.TryCancelled(
                asyncWithCatch,
                this.OnCancelled
                )
            this.SetJobState(JobState.Running)
            Async.StartImmediate(asyncWithCanceleld, cts.Token)
        | state -> raise <| new InvalidJobStateException(state, ErrorMessages.JobAlreadyStarted)

    override this.StopJob () =
        match this.JobStateInfo.State with
        | JobState.Running ->
            this.SetJobState(JobState.Stopping)
            cts.Cancel()
        | _ -> this.SetJobState(JobState.Stopped)

    override this.Dispose(disposing) =
        match this.JobStateInfo.State with
        | JobState.Running -> this.StopJob()
        | JobState.Stopping -> ()
        // Set only does anything if we haven't already stopped
        | _ -> this.SetJobState(JobState.Stopped)
        cts.Dispose()
        base.Dispose(disposing)