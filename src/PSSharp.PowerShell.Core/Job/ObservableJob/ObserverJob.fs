namespace PSSharp
open System
open System.Management.Automation

type internal DelegateObserver<'T> (onNext: 'T -> unit, onError: Exception -> unit, onCompleted: unit -> unit) =
    interface IObserver<'T> with
        member _.OnNext t = onNext t
        member _.OnError e = onError e
        member _.OnCompleted () = onCompleted()

[<AbstractClass>]
type ObserverJob (command, name) =
    inherit JobBase(command, name)

type ObserverJob<'T> (command, name) =
    inherit ObserverJob(command, name)

    member val private Cancellation: IDisposable option = None with get, set

    member this.TryUnsubscribe() =
        match this.Cancellation with
        | Some c -> 
            c.Dispose()
            this.Cancellation <- None
            true
        | None -> false

    member private this.OnNext next =
        this.Output.Add next

    member private this.OnError exn =
        let er = new ErrorRecord(
            exn,
            exn.GetType().Name,
            ErrorCategory.NotSpecified,
            null
            )
        this.Error.Add er
        this.TryUnsubscribe() |> ignore
        this.SetJobState(JobState.Failed)

    member private this.OnCompleted () =
        this.SetJobState(JobState.Completed)

    override this.StopJob () =
        this.SetJobState(JobState.Stopping)
        this.TryUnsubscribe() |> ignore
        this.SetJobState(JobState.Stopped)

    member this.StartJob (observable: IObservable<_>) =
        match this.JobStateInfo.State with
        | JobState.NotStarted ->
            this.SetJobState(JobState.Running)
            let observer = new DelegateObserver<_>(pso >> this.OnNext, this.OnError, this.OnCompleted)
            let cancellation = observable.Subscribe(observer)
            this.Cancellation <- Some cancellation
        | state -> raise <| new InvalidJobStateException(state, ErrorMessages.JobAlreadyStarted)
