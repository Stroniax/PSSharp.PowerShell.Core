namespace PSSharp
open System
open System.Management.Automation
open System.Net
open System.Net.NetworkInformation
open System.Runtime.InteropServices

[<Struct>]
type PingCompletionState =
    | Error of error : exn
    | Success of reply : PingReply
    | Cancelled
    /// Gets a PingCompletionState representing the information contained by the PingCompletedEventArgs.
    [<CompiledName("OfEventArgs")>]
    static member ofEventArgs(e : PingCompletedEventArgs) =
        match e.Error |> ValueOption.ofObj with
        | ValueSome error -> Error error
        | ValueNone ->
            match e.Cancelled with
            | true -> Cancelled
            | false -> Success e.Reply

/// Job that represents an asynchronous, repeating ping operation until successful
/// or the job is stopped.
[<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
type PingJob (hostNameOrIp : string, timeout : int, maximumAttempts : int, buffer : byte array voption, options : PingOptions voption) =
    inherit Job2Base ()

    /// Buffer to send when options are provided but a buffer is not provided.
    static let mutable defaultBuffer : byte array voption = ValueNone
    /// Buffer to send when options are provided but a buffer is not provided.
    static let getDefaultBuffer () =
        match defaultBuffer with
        | ValueSome buffer -> buffer
        | ValueNone ->
            // this is the buffer sent by the ping when an overload without the buffer parameter is called
            let buffer = [|
                97uy; 98uy; 99uy; 100uy; 101uy; 102uy; 103uy; 104uy; 105uy; 106uy; 107uy;
                108uy; 109uy; 110uy; 111uy; 112uy; 113uy; 114uy; 115uy; 116uy; 117uy; 118uy;
                119uy; 97uy; 98uy; 99uy; 100uy; 101uy; 102uy; 103uy; 104uy; 105uy
                |]
            defaultBuffer <- ValueSome buffer
            buffer

    /// Ping timeout. Default is 5 seconds https://stackoverflow.com/a/17162728/8994350
    [<Literal>]
    static let defaultTimeout = 5000

    /// Ping sender for the job
    let ping = new Ping ()
    /// Lock for job state & ping
    let syncLock = new obj ()
    /// Gets a buffer when required
    let getRequiredBuffer () =
        match buffer with
        | ValueSome some -> some
        | ValueNone -> getDefaultBuffer()
    let mutable attemptCount = 0
    do
        if timeout <= 0 then
            new ArgumentOutOfRangeException(nameof timeout) |> raise
        if maximumAttempts <= 0 then
            new ArgumentOutOfRangeException(nameof maximumAttempts) |> raise

    new (
        hostNameOrIp : string,
        [<Optional; DefaultParameterValue(5000)>] timeout : int,
        [<Optional; DefaultParameterValue(Int32.MaxValue)>] maximumAttempts : int,
        [<Optional; DefaultParameterValue(null : byte array)>] buffer : (byte array),
        [<Optional; DefaultParameterValue(null : PingOptions)>] options : PingOptions) =
        new PingJob(
            hostNameOrIp,
            timeout,
            maximumAttempts,
            buffer |> ValueOption.ofObj,
            options |> ValueOption.ofObj
            )
    member private this.TryIncrementAttemptCount () =
        attemptCount <- attemptCount + 1
        match attemptCount >= maximumAttempts with
        | true -> 
            let ex = new Exception(ErrorMessages.PingJobMaximumRetry)
            let er = new ErrorRecord(
                ex,
                nameof ErrorMessages.PingJobMaximumRetry,
                ErrorCategory.LimitsExceeded,
                maximumAttempts
                )
            er.ErrorDetails <- new ErrorDetails(
                String.Format(ErrorMessages.PingJobMaximumRetryInterpolated, maximumAttempts)
                )
            this.Error.Add er
            this.SetJobState JobState.Failed
            false
        | false -> true

    /// Send the ping with the parameters as configured during object construction.
    member private this.SendPing () =
        if this.TryIncrementAttemptCount () then
            match options with
            | ValueSome options -> ping.SendAsync(hostNameOrIp, timeout, getRequiredBuffer(), options, null)
            | ValueNone -> ping.SendAsync(hostNameOrIp, timeout, null)

    /// Update the current JobState of applicable (Stopping/Suspending), or send the next ping request.
    member private this.UpdateJobStateOrContinue () =
        match this.JobStateInfo.State with
        | JobState.Stopping -> 
            this.SetJobState(JobState.Stopped)
        | JobState.Suspending ->
            Console.Write "The job was suspending ..."
            this.SetJobState(JobState.Suspended)
        | JobState.Running ->
            this.SendPing ()
        | state -> 
            let ex = new InvalidJobStateException(
                state,
                ErrorMessages.PingJobUnhandledStateForContinuation
                )
            let er = new ErrorRecord(
                    ex,
                    nameof ErrorMessages.PingJobUnhandledStateForContinuation,
                    ErrorCategory.InvalidData,
                    state
                )
            er.ErrorDetails <- new ErrorDetails(
                String.Format(ErrorMessages.PingJobUnhandledStateForContinuationInterpolated, state)
                )
            this.Error.Add er
            this.SetJobState JobState.Failed

    /// Complete successfully with the result
    member private this.SucceedWith result =
        result |> pso |> this.Output.Add
        this.SetJobState JobState.Completed
        ping.Dispose ()

    /// Inner function to run inside a lock statement when the PingCompleted event is raised.
    member private this.OnPingCompletedLocked (eventArgs : PingCompletedEventArgs) =
        new InformationRecord(
            eventArgs,
            this.Name
            )
        |> this.Information.Add
        match eventArgs |> PingCompletionState.ofEventArgs with
        | Success reply -> 
            this.SucceedWith reply
        | Error e when (e :? ObjectDisposedException) ->
            let er = new ErrorRecord(e,
                nameof ObjectDisposedException,
                ErrorCategory.InvalidData,
                eventArgs)
            this.Error.Add(er)
            this.SetJobState(JobState.Failed)
        | Cancelled
        | Error _ -> 
            this.UpdateJobStateOrContinue ()

    /// Handle the PingCompleted event. If the job is running and the ping was unsuccessful,
    /// a new ping will be sent; otherwise, the job will halt as appropriate to the current
    /// job state.
    member private this.OnPingCompleted (sender : obj) (eventArgs : PingCompletedEventArgs) =
        lock (syncLock) (fun () -> this.OnPingCompletedLocked(eventArgs))

    /// The location the ping is being sent to.
    override _.Location = hostNameOrIp
    /// The maximum number of times to send the ping before the job fails.
    member _.MaximumAttempts = maximumAttempts
    /// The number of times the ping was attempted.
    member _.AttemptCount = attemptCount

    /// Starts the current PingJob.
    override this.StartJob () =
        match this.JobStateInfo.State with
        | JobState.NotStarted ->
            let eventHandler = new PingCompletedEventHandler(this.OnPingCompleted)
            ping.PingCompleted.AddHandler(eventHandler)
            this.SetJobState(JobState.Running)
            this.SendPing()
        | state -> 
            new InvalidJobStateException(state, ErrorMessages.JobAlreadyStarted)
            |> raise

    /// Terminates the current PingJob.
    override this.StopJob(force, reason) =
        this.StopJob(force, ValueOption.ofObj reason)
    member private this.SetStoppingLocked() =
        this.SetJobState(JobState.Stopping)
    member this.StopJob(force, ?reason) =
        match force with
        | true -> this.SetJobState(JobState.Stopped)
        | false -> lock (syncLock) this.SetStoppingLocked
        ping.SendAsyncCancel()
        
    /// Inner function to run while locked to suspend the ping job.
    member private this.SuspendJobLocked () =
        match this.JobStateInfo.State with
        | JobState.Suspended
        | JobState.Suspending -> ()
        | JobState.Running ->
            this.SetJobState(JobState.Suspending)
            ping.SendAsyncCancel()
        | state ->
            new InvalidJobStateException(state, PSSharp.JobStatus.NotSuspendable)
            |> raise
    /// Attempts to suspend the PingJob if it is running.
    override this.SuspendJob (force, reason) =
        lock (syncLock) this.SuspendJobLocked

    //// Inner function to run while locked to resume the PingJob.
    member private this.ResumeJobLocked () =
        match this.JobStateInfo.State with
        | JobState.Suspending -> this.SetJobState(JobState.Running)
        | JobState.Running -> ()
        | JobState.Suspended ->
            this.SetJobState(JobState.Running)
            this.SendPing()
        | state ->
            new InvalidJobStateException(state, ErrorMessages.JobStatusNotResumable)
            |> raise
    /// Attempts to resume the PingJob if it is suspended.
    override this.ResumeJob() =
        lock syncLock this.ResumeJobLocked

    /// Free unmanaged resources.
    override _.Dispose(disposing) =
        ping.Dispose()
        base.Dispose(disposing)
