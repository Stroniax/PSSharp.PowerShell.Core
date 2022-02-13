namespace PSSharp
open System
open System.Threading
open System.Management.Automation
open System.Runtime.InteropServices
open System.Reflection

/// <summary>
/// PowerShell job wrapper for an F# Async computation represented by the
/// <see cref="Async{_}"/> type.
/// </summary>
[<NoEquality; NoComparison; AutoSerializable(false)>]
type FSAsyncJob internal (computation : Async<unit>, command : string, jobName : string) as this =
    inherit Job2Base(command, jobName)
    static let reflectedCreate =
        typeof<FSAsyncJob>.GetMethods(BindingFlags.Public ||| BindingFlags.Static)
            |> Array.find (
                fun method ->
                    method.Name = "Create"
                    && method.IsGenericMethod
                )
    let cts = new CancellationTokenSource ()
    let computation =
        async {
            use! cancellation = Async.OnCancel(this.SetStopped)
            try
                do! computation
                this.SetCompleted()
            with
            | e ->
                ErrorRecord(e, "AsyncComputationFailed", ErrorCategory.NotSpecified, computation)
                |> this.Error.Add
                this.SetFailed()
        }

    static member Create(
            computation : Async<unit>,
            [<Optional; DefaultParameterValue(null : string)>] command : string,
            [<Optional; DefaultParameterValue(null : string)>] jobName : string
        ) =
        new FSAsyncJob(
            computation,
            command,
            jobName
            )
    static member Create(
            computation : Async<'a>,
            [<Optional; DefaultParameterValue(null : string)>] command : string,
            [<Optional; DefaultParameterValue(null : string)>] jobName : string
        ) =
        new FSAsyncJob<'a>(
            computation,
            command,
            jobName
        )
    /// Creates a new job from a boxed computation. Returns None if the computation object
    /// is not an instance of Async<_>.
    static member TryCreateFromObj(
            computation : obj,
            [<Optional; DefaultParameterValue(null :  string)>] command : string,
            [<Optional; DefaultParameterValue(null : string)>] jobName : string
        ) =
        match computation with
        | null -> None
        | :? Async<unit> as unitComputation ->
            Some <| FSAsyncJob.Create(unitComputation, command, jobName)
        | _ ->
            let t = computation.GetType()
            if not t.IsGenericType then None else
            if t.GetGenericTypeDefinition() <> typedefof<Async<_>> then None else
            let createMethod = reflectedCreate.MakeGenericMethod(t.GetGenericArguments()[0])
            createMethod.Invoke(null, [|computation ; command ; jobName|])
            :?> FSAsyncJob
            |> Some
    /// Creates a new job from a boxed computation. Returns false if the computation object
    /// is not an instance of Async<_>.
    static member TryCreateFromObj(
            computation : obj,
            [<Optional; DefaultParameterValue(null :  string)>] command : string,
            [<Optional; DefaultParameterValue(null : string)>] jobName : string,
            job : FSAsyncJob outref
        ) =
        match FSAsyncJob.TryCreateFromObj(computation, command, jobName) with
        | Some createdJob -> job <- createdJob ; true
        | None -> false

    member private this.SetStopped () = this.SetJobState(JobState.Stopped)
    member private this.SetCompleted () = this.SetJobState(JobState.Completed)
    member private this.SetFailed () = this.SetJobState(JobState.Failed)
    abstract IsOutputExpected : bool
    default _.IsOutputExpected = false

    override this.StartJob () =
        match this.JobStateInfo.State with
        | JobState.NotStarted ->
            this.SetJobState(JobState.Running)
            Async.Start(computation, cts.Token)
        | state -> new InvalidJobStateException(state, ErrorMessages.JobAlreadyStarted) |> raise

    override this.StopJob (force, reason) =
        match this.JobStateInfo.State with
        | JobState.Running ->
            this.SetJobState(JobState.Stopping)
            cts.Cancel ()
        | JobState.Stopping
        | JobState.Stopped -> ()
        | state -> raise <| new InvalidJobStateException(state, ErrorMessages.JobNotRunning)

    override this.Dispose (disposing) =
        cts.Dispose()
        base.Dispose(disposing)

and [<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
    FSAsyncJob<'a> internal (computation : Async<'a>, command, jobName) as this =
    inherit FSAsyncJob (
            async {
                let! result = computation
                result |> pso |> this.Output.Add
            },
            command,
            jobName
        )
        do base.PSJobTypeName <- nameof FSAsyncJob

        override _.IsOutputExpected = true