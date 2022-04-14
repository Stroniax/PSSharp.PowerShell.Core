namespace PSSharp
open System
open System.Threading
open System.Threading.Tasks
open System.Collections
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Collections.Concurrent

/// Encompasses a suite of tooling for the reactive asynchronous pubisher-subscriber method through the
/// IObservable<_> and IObserver<_> interfaces.
module Reactive =
    [<assembly: Extension>]
    do()

    /// Run a function and ignore any raised ObjectDisposedExceptions.
    [<CompiledName("CallWithoutObjectDisposed")>]
    let inline private callWithoutObjectDisposed fn =
        try fn()
        with :? ObjectDisposedException -> ()

    /// Dispose of an object if it implements IDisposable or IAsyncDisposable
    [<CompiledName("Dispose")>]
    let private dispose (v: obj) =
        match v with
        | :? IDisposable as d -> d.Dispose()
        | :? IAsyncDisposable as d -> d.DisposeAsync() |> Async.AwaitTaskLike |> Async.RunSynchronously
        | _ -> ()

    /// Registers disposing of the cancellation parameter (an #IDisposable implementation) to the
    /// cancellation of the token parameter.
    [<CompiledName("RegisterCancellation")>]
    let private registerCancellation (token: CancellationToken) (cancellation: #IDisposable) =
        token.Register(dispose, cancellation)

    /// A singleton instance of an IDisposable implementation with no effect upon disposal.
    [<CompiledName("Undisposable")>]
    let private undisposable = { new IDisposable with member _.Dispose () = () }
    /// Creates a new IDisposable instance which runs fn when it is disposed. The resulting
    /// IDisposable is not automatically thread-safe, and will re-run fn when disposed multiple
    /// times.
    [<CompiledName("Disposable")>]
    let private disposable fn = { new IDisposable with member _.Dispose () = fn () }

    /// Throws the single exception in the list if there is one exception, or if there are multiple exceptions
    /// throws an AggregateException.
    [<CompiledName("RaiseSmallest")>]
    let inline raiseSmallest (errors: #IReadOnlyList<exn>) =
        if errors.Count = 1 then raise <| errors[0]
        elif errors.Count > 1 then raise <| new AggregateException(errors)

    /// Runs a function over each of a series of parameters, in parallel.
    /// Fails with AggregateException if any function call fails; regardless, the function
    /// is started for each parameter.
    [<CompiledName("ForEachParallel")>]
    let private forEachParallel (fn: 'a -> unit) (p: 'a seq) =
        let ers = new ConcurrentBag<exn>()
        Parallel.ForEach(p, fun item -> try fn item with e -> ers.Add e) |> ignore
        if ers.Count > 0 then raise <| new AggregateException(ers)

    /// Runs a series of functions with a given parameter, in parallel.
    /// Fails with AggregateException if any function call fails; regardless, all functions will be started
    /// with the given parameter.
    [<CompiledName("RunParallel")>]
    let private runParallel (functions: ('a -> unit) seq) (p: 'a) =
        let ers = new ConcurrentBag<exn>()
        Parallel.ForEach(functions, fun fn -> try fn p with e -> ers.Add e) |> ignore
        if ers.Count > 0 then raise <| new AggregateException(ers)

    /// Union of any values to provide to an observable function.
    type ObserverData<'T> =
        /// The sequence has completed.
        | ObserverCompleted
        /// The sequence failed with an exception.
        | ObserverError of error: exn
        /// The sequence received new data.
        | ObserverNext of data: 'T

        /// Throws an error if this object is not an ObserverNext. Otherwise, returns the data associated
        /// with this value.
        member this.GetNextData() : 'T =
            match this with
            | ObserverCompleted -> invalidOp ErrorMessages.ObservableCompletedBeforeNext
            | ObserverError error -> raise error
            | ObserverNext data -> data
        /// Throws the error if this object is an ObserverError.
        member this.FailIfError() =
            match this with
            | ObserverError error -> raise error
            | _ -> ()
    
    /// Module for working with the ObserverData<'T> union type.
    module ObserverData =
        [<CompiledName("Bind")>]
        let bind (binder: 'T -> ObserverData<'U>) data =
            match data with
            | ObserverNext v -> binder v
            | ObserverError e -> ObserverError e
            | ObserverCompleted -> ObserverCompleted
        [<CompiledName("Map")>]
        let map (mapping: 'T -> 'U) data =
            data |> bind (mapping >> ObserverNext)
        [<CompiledName("Get")>]
        let get data =
            match data with
            | ObserverCompleted -> invalidOp ErrorMessages.ObserverDataCompleted
            | ObserverError e -> raise <| InvalidOperationException(ErrorMessages.ObserverDataFailed, e)
            | ObserverNext v -> v

    /// Contract for an observable that runs once and exposes continuation registrations to be
    /// run when the observable is finished.
    type IAwaitableObservable<'T> =
        inherit IObservable<'T>
        /// Registers a continuation function to be run when the observable completes. Runs the
        /// continuation immediatly and returns true if the observable is already finished.
        abstract member OnFinished : continuation: (unit -> unit) -> bool
        /// Gets the currently known result of the observable sequence, or ObserverNext() if the
        /// observable is not currently completed.
        abstract member GetResult : unit -> ObserverData<unit>

    type 'a oseq = IObservable<'a>
    type 'a obs = IObserver<'a>
    type IConnectibleObservable<'T> =
        abstract Connect: unit -> IObservable<'T>

    /// Functional Observable implementation. Runs the subscription function upon subscription in a thread pool
    /// thread immediately when an observer is subscribed.
    [<Sealed>]
    type DelegateObservable<'T>(onSubscribed: IObserver<'T> -> Async<unit>) =
        member _.Subscribe observer =
            let cts = new CancellationTokenSource()
            let comp = async {
                use cts = cts
                do! onSubscribed observer
            }
            Async.Start(comp, cts.Token)
            {
                new IDisposable with
                member _.Dispose() =
                    try cts.Cancel()
                    with :? ObjectDisposedException -> ()
            }

        interface IObservable<'T> with
            member this.Subscribe observer = this.Subscribe observer

    type SubjectObservable<'T> internal () =
        let mutable _last = ObserverNext Unchecked.defaultof<'T>
        let _subscriptions = new List<IObserver<'T>>()
        let _continuations = new List<unit -> unit>()
        let lock fn = lock _subscriptions fn

        let failIfFinished() =
            match _last with
            | ObserverNext _ -> ()
            | ObserverCompleted -> invalidOp ErrorMessages.ObserverFinished
            | ObserverError e -> raise <| new InvalidOperationException(ErrorMessages.ObserverFinished, e)
        
        let runIfNotFinished fn =
            failIfFinished()
            lock (fun () ->
                failIfFinished()
                fn()
                )

        let executeOnFinished () =
            let ers = new List<exn>()
            for cont in _continuations do
                try cont()
                with e -> ers.Add e
            _subscriptions.Clear()
            _continuations.Clear()
            ers
            
        /// Gets the current result state of the observable, or ObserverNext() if it is not finished.
        member this.GetResult() =
            match _last with
            | ObserverCompleted -> ObserverCompleted
            | ObserverError e -> ObserverError e
            | ObserverNext _ ->
                lock (fun () ->
                    match _last with
                    | ObserverError e -> ObserverError e
                    | ObserverCompleted -> ObserverCompleted
                    | ObserverNext _ -> ObserverNext ()
                    )

        member this.IsFinished =
            match this.GetResult() with
            | ObserverNext _ -> false
            | _ -> true

        member this.IsFaulted =
            match this.GetResult() with
            | ObserverError _ -> true
            | _ -> false

        member this.IsCompletedSuccessfully =
            match this.GetResult() with
            | ObserverCompleted -> true
            | _ -> false

        member this.TryGetError() =
            match this.GetResult() with
            | ObserverError e -> Some e
            | _ -> None

        member this.FailIfNotFinished() =
            match this.GetResult() with
            | ObserverNext _ -> invalidOp ErrorMessages.ObserverNotFinished
            | _ -> ()

        member this.FailIfNotSuccessful() =
            match this.GetResult() with
            | ObserverError e -> raise e
            | ObserverNext _ -> invalidOp ErrorMessages.ObserverNotFinished
            | _ -> ()

        member this.FailIfFinished() =
            match this.GetResult() with
            | ObserverError e -> raise e
            | ObserverCompleted -> invalidOp ErrorMessages.ObserverFinished
            | ObserverNext _ -> ()

        /// <summary>
        /// Queues a continuation function to be executed when the observable is finished.
        /// If the observable is already finished, the continuation is run synchronously
        /// and the function returns <see langword="true"/>.
        /// </summary>
        member this.OnFinished continuation =
            let run = lock (fun () ->
                    match _last with
                    | ObserverNext _ -> _continuations.Add continuation; false
                    | _ -> true
                    )
            if run then continuation()
            run

        /// Subscribes a listener observer to the subject.
        member this.Subscribe observer =
            let unsubscription = 
                lock (fun () ->
                    _last
                    |> ObserverData.map (fun _ ->
                            _subscriptions.Add observer
                            {
                                new IDisposable with
                                member _.Dispose() = 
                                    lock (fun () ->
                                        _subscriptions.Remove observer
                                        |> ignore
                                        )
                            }
                        )
                    )
            match unsubscription with
            | ObserverNext unreg -> unreg
            | ObserverCompleted -> observer.OnCompleted(); undisposable
            | ObserverError e -> observer.OnError e; undisposable

        member internal this.OnNext value =
            runIfNotFinished (fun () ->
                _last <- ObserverNext value
                let ers = new List<exn>()
                for obs in _subscriptions do
                    try obs.OnNext value
                    with e -> ers.Add e
                raiseSmallest ers
                )

        member internal this.OnCompleted () =
            runIfNotFinished (fun () ->
                _last <- ObserverCompleted
            )
            let ers = new List<exn>()
            for obs in _subscriptions do
                try obs.OnCompleted ()
                with e -> ers.Add e
            ers.AddRange(executeOnFinished())
            raiseSmallest ers

        member internal this.OnError error =
            runIfNotFinished (fun () ->
                _last <- ObserverError error
            )
            let ers = new List<exn>()
            for obs in _subscriptions do
                try obs.OnError error
                with e -> ers.Add e
            ers.AddRange(executeOnFinished())
            raiseSmallest ers

        interface IObservable<'T> with
            member this.Subscribe observer = this.Subscribe observer

    type SubjectObserver<'T> internal (observable: SubjectObservable<'T>) =
        member this.Observable = observable

        member this.OnNext value = observable.OnNext value
        member this.OnError error = observable.OnError error
        member this.OnCompleted () = observable.OnCompleted ()

        interface IObserver<'T> with
            member this.OnNext value = this.OnNext value
            member this.OnError error = this.OnError error
            member this.OnCompleted () = this.OnCompleted ()

    type SubjectObservable<'T> with
        static member Create(observable: _ outref, observer: _ outref) =
            observable <- new SubjectObservable<'T>()
            observer <- new SubjectObserver<'T>(observable)
        static member Create() =
            let observable = new SubjectObservable<'T>()
            let observer = new SubjectObserver<'T>(observable)
            {|
                Observable = observable
                Observer = observer
            |}

    type Subject<'T> internal (observable: SubjectObservable<'T>, observer: SubjectObserver<'T>) =
        new() =
            let observable = new SubjectObservable<'T>()
            let observer = new SubjectObserver<'T>(observable)
            new Subject<'T>(observable, observer)

        member this.Observer = observer
        member this.Observable = observable

        member this.OnFinished continuation = observable.OnFinished continuation
        member this.Subscribe observer = observable.Subscribe observer
        member this.OnNext value = observer.OnNext value
        member this.OnCompleted() = observer.OnCompleted()
        member this.OnError error = observer.OnError error

        interface IObservable<'T> with
            member this.Subscribe observer = this.Subscribe observer
        interface IObserver<'T> with
            member this.OnNext value = this.OnNext value
            member this.OnError error = this.OnError error
            member this.OnCompleted () = this.OnCompleted ()

    /// Hides an IObservable by wrapping it into a new IObservable implementation.
    [<Sealed>]
    type ObservableReference<'T> (ref: IObservable<'T>) =
        member _.Subscribe obs = ref.Subscribe obs
        interface IObservable<'T> with
            member _.Subscribe obs = ref.Subscribe obs

    /// Functional Observable implementation. Runs the subscription function upon subscription in a thread pool
    /// thread when created or when the first observer is subscribed, and retains the first retainCount number of
    /// items to publish immediately to future subscriptions.
    [<Sealed>]
    type MemorizedObservable<'T>(onSubscribed: IObserver<'T> -> Async<unit>, retainCount: int, startImmediately: bool) =
        class
        end

    /// Functional observer implementation.
    [<Sealed>]
    type DelegateObserver<'T> (?onNext: 'T -> unit, ?onCompleted: unit -> unit, ?onError: exn -> unit) =
        let sync =
            match onNext, onCompleted, onError with
                // Subscription for side effects only may have no functions.
                | None, None, None -> obj()
                | Some l, _, _ -> l
                | _, Some l, _ -> l
                | _, _, Some l -> l
        let lock fn = lock sync fn
        let mutable _isCompleted = false
        let mutable _exn = null
        let _onCompletedCallback = ConcurrentBag<unit -> unit>()
        /// Test a predicate. If the predicate is evaluated to false, lock and rest the
        /// predicate again.
        let lockedTest predicate = predicate() || lock predicate

        /// Only call after setting _isCompleted while in lock when not already completed.
        /// Runs all continuations and clears the callback bag.
        let runContinuations() =
            try runParallel _onCompletedCallback ()
            finally _onCompletedCallback.Clear()

        member this.IsFinished = lockedTest (fun () -> _isCompleted || _exn <> null)
        member this.IsCompletedSuccessfully = lockedTest (fun () -> _isCompleted)
        member this.IsFaulted = lockedTest (fun () -> _exn <> null)
        member this.TryGetError() = Option.ofObj _exn
        member this.TryGetResult() =
            lock (fun () ->
                match _isCompleted, _exn with
                | true, null -> ObserverCompleted
                | false, null -> ObserverNext()
                | _, e -> ObserverError e
                )

        /// Subscribe a continuation function for when the observer finishes. Returns true
        /// if the continuation was run immediately.
        member this.Subscribe callback =
            let run =
                lock (fun () ->
                    match _isCompleted with
                    | true -> true
                    | false ->
                        _onCompletedCallback.Add callback
                        false
                    )
            if run then callback()
            run

        /// Moves the observer to the next state.
        member this.OnNext value =
            let callback = lock (fun () ->
                match _isCompleted with
                | false -> None
                | true -> onNext
                )
            match callback with
            | Some fn -> fn value
            | None -> ()

        /// Moves the observer to the completed state.
        member this.OnCompleted() =
            let nowFinished = lock (fun () ->
                match _isCompleted with
                | false ->
                    _isCompleted <- true
                    true
                | _ -> false
                )
            if nowFinished then
                let ers = new List<exn>()
                match onCompleted with
                | Some fn -> try fn() with e -> ers.Add e
                | None -> ()
                try runContinuations() with e -> ers.Add e
                raiseSmallest ers
                
        /// Moves the observer to the error state.
        member this.OnError error =
            let nowFinished = lock (fun () ->
                match _isCompleted with
                | false ->
                    _isCompleted <- true
                    _exn <- error
                    true
                | _ -> false
                )
            if nowFinished then
                let ers = new List<exn>()
                match onError with
                | Some fn -> try fn error with e -> ers.Add e
                | None -> ()
                try runContinuations() with e -> ers.Add e
                raiseSmallest ers

        /// Creates a wait handle that will be set when the observer completes.
        member this.TryCreateWaitHandle () : ObserverData<WaitHandle> =
            lock (fun () ->
                match _isCompleted with
                    | true when _exn <> null -> ObserverError _exn
                    | true -> ObserverCompleted
                    | false ->
                        let wh = new ManualResetEvent(_isCompleted)
                        wh.Set >> ignore |> this.Subscribe |> ignore
                        ObserverNext wh
                )

        interface IObserver<'T> with
            member this.OnNext value = this.OnNext value
            member this.OnCompleted() = this.OnCompleted()
            member this.OnError error = this.OnError error
    
    /// Awaiter for async-await with DelegateObserver.
    [<Struct>]
    type DelegateObserverAwaiter<'T> (obs : DelegateObserver<'T>) =
        member this.OnCompleted continuation = obs.Subscribe continuation |> ignore
        member this.OnCompleted (continuation: Action) = obs.Subscribe continuation.Invoke |> ignore
        member this.IsCompleted = obs.IsFinished
        member this.GetResult() =
            match obs.TryGetResult() with
            | ObserverNext _ -> invalidOp ErrorMessages.ObserverNotFinished
            | ObserverError exn -> raise exn
            | ObserverCompleted -> ()

        interface INotifyCompletion with
            member _.OnCompleted continuation = obs.Subscribe continuation.Invoke |> ignore

    /// Awaiter-related members of DelegateObserver
    type DelegateObserver<'T> with
        /// Gets an awaiter for C# task-like awaiting for the DelegateObserver<'T> to be completed.
        member this.GetAwaiter() = new DelegateObserverAwaiter<'T>(this)
        /// Waits for the observer to finish. Fails if the observer received an error.
        member this.Wait() = this |> Async.AwaitTaskLike |> Async.RunSynchronously
        member this.AsyncWait() = this |> Async.AwaitTaskLike
        member this.WaitAsync(cancellationToken: CancellationToken) =
            match this.TryGetResult() with
            | ObserverCompleted -> ValueTask.CompletedTask
            | ObserverError exn -> ValueTask.FromException exn
            | ObserverNext _ -> 
                cancellationToken.ThrowIfCancellationRequested()
                this |> Async.AwaitTaskLike |> Async.StartImmediateAsTask |> ValueTask

    /// An IDisposable wrapper around a delegate function that is called when the subscription is disposed.
    type DelegateSubscription(subscription: unit -> unit) =
        static let s_none = new DelegateSubscription(id)

        /// A singleton instance of a subscription that does nothing.
        static member None = s_none
        
        /// Creates a DelegateSubscription from the subscription action.
        static member Create(subscription: Action) = new DelegateSubscription(subscription.Invoke)
        /// Creates a DelegateSubscription from the subscription function.
        static member Create(subscription: unit -> unit) = new DelegateSubscription(subscription)
        /// Creates a DelegateSubscription that will only call the subscription function once.
        static member CreateUnreusable(subscription: unit -> unit) =
            let sync = obj()
            let mutable run = false
            new DelegateSubscription(fun () ->
                if run then () else
                lock sync (fun () ->
                    if run then () else
                    run <- true
                    subscription()
                    )
                )

        interface IDisposable with
            member _.Dispose() = subscription()

    type ObservableEventEventHandler<'TArgs> = delegate of sender: obj * args: 'TArgs -> unit

    type FirstValueObserver<'T>() =
        let mutable _last = None
        let _continuations = new List<unit -> unit>()
        let onFinished() =
            try runParallel _continuations ()
            finally _continuations.Clear()

        member this.TryGetResult() = _last
        member this.TryCreateWaitHandle() : WaitHandle option =
            lock _continuations (fun () ->
                match _last with
                | None ->
                    let wh = new ManualResetEvent(false)
                    wh.Set >> ignore |> _continuations.Add
                    Some wh
                | _ -> None
                )

        member this.OnCompleted () =
            let fin = lock _continuations (fun () ->
                match _last with
                | None ->
                    _last <- Some ObserverCompleted
                    true
                | _ -> false
                )
            if fin then onFinished()
        member this.OnError error =
            let fin = lock _continuations (fun () ->
                match _last with
                | None ->
                    _last <- error |> ObserverError |> Some
                    true
                | _ ->
                    false
                )
            if fin then onFinished()
        member this.OnNext value =
            let fin = lock _continuations (fun () ->
                match _last with
                | None ->
                    _last <- value |> ObserverNext |> Some
                    true
                | _ -> false
                )
            if fin then onFinished()

        interface IObserver<'T> with
            member this.OnNext value = this.OnNext value
            member this.OnError error = this.OnError error
            member this.OnCompleted () = this.OnCompleted ()
            
    /// Represents an observable sequence with the latest event data available. Can also be enumerated
    /// asynchronnously to get the most recent event data. If multiple values are provided while
    /// processing a previous value, async enumeration will only provide the latest again when the next
    /// call is made to MoveNextAsync() without enumerating values from in between.
    type ObservableEvent<'TArgs> =
        val private _handlers: System.Collections.Generic.List<(obj * 'TArgs -> unit)>
        val private _completeObservers: System.Collections.Generic.HashSet<(unit -> unit)>
        val mutable private _current: 'TArgs
        val mutable private _isDisposed : bool

        new(initial: 'TArgs, publisher: ('TArgs -> unit) outref) as this =
            publisher <- fun t -> this._handlers |> Seq.iter (fun h -> h(this, t))
            {
                _current = initial
                _isDisposed = false
                _handlers = System.Collections.Generic.List<_>()
                _completeObservers = System.Collections.Generic.HashSet<_>()
            }

        [<CLIEvent>]
        member this.Next : IEvent<_, _> = this
        member this.Current = this._current
        
        member private this.AddHandler handler =
            if this._isDisposed then () else
            lock this._handlers (fun () -> 
                if not this._isDisposed then this._handlers.Add handler
                )
        member private this.RemoveHandler handler =
            if this._isDisposed then () else
            lock this._handlers (fun () ->
                if not this._isDisposed then this._handlers.Remove handler |> ignore
                )

        member this.Subscribe (observer: #IObserver<'TArgs>) : IDisposable =
            if this._isDisposed then
                observer.OnNext this._current
                observer.OnCompleted()
                DelegateSubscription.None
            else
                lock this._handlers (fun () ->
                    if this._isDisposed then
                        observer.OnNext this._current
                        observer.OnCompleted()
                        DelegateSubscription.None
                    else
                        let handler = fun (sender, args) -> observer.OnNext args
                        let sub = fun () -> observer.OnCompleted()
                        this._completeObservers.Add sub |> ignore
                        this.AddHandler handler
                        ThreadPool.QueueUserWorkItem(fun _ -> observer.OnNext this._current) |> ignore
                        {   new IDisposable with
                            member _.Dispose () =
                                this._completeObservers.Remove sub |> ignore
                                this.RemoveHandler handler
                            }
                )

        interface IEvent<ObservableEventEventHandler<'TArgs>, 'TArgs>
        interface IObservable<'TArgs> with
            member this.Subscribe observer = this.Subscribe observer
        interface IDelegateEvent<ObservableEventEventHandler<'TArgs>> with
            member this.AddHandler handler = this.AddHandler handler.Invoke
            member this.RemoveHandler handler = this.RemoveHandler handler.Invoke
        interface IDisposable with
            member this.Dispose() =
                if this._isDisposed then () else
                lock this._handlers (fun () ->
                    if this._isDisposed then () else
                    this._isDisposed <- true
                    )
                this._handlers.Clear()
                try this._completeObservers |> Seq.iter (fun v -> v())
                finally this._completeObservers.Clear()

    type IObserver<'T> with
        /// Publishes an item, error, or completion to an observer.
        /// Returns a value that indicates if the observer was finished with the provided data value.
        member this.On observerData =
            match observerData with
            | ObserverCompleted -> this.OnCompleted(); true
            | ObserverNext data -> this.OnNext data; false
            | ObserverError error -> this.OnError error; true

    type IObservable<'T> with
        member this.Subscribe(?onNext: 'T -> unit, ?onCompleted: unit -> unit, ?onError: exn -> unit) : IDisposable =
            let obs = new DelegateObserver<'T>(?onNext = onNext, ?onCompleted = onCompleted, ?onError = onError)
            this.Subscribe(obs)
        member this.Subscribe(?onNext: 'T -> unit, ?onCompleted: unit -> unit, ?onError: exn -> unit, ?cancellationToken: CancellationToken) : unit =
            let obs = new DelegateObserver<'T>(?onNext = onNext, ?onCompleted = onCompleted, ?onError = onError)
            let rm = this.Subscribe(obs)
            match cancellationToken with
            | Some ct ->
                let unreg = registerCancellation ct rm
                obs.Subscribe (unreg.Unregister >> ignore) |> ignore
            | None -> ()
        /// Waits for an IObservable<_> sequence to finish.
        member this.AsyncWait() : Async<unit> =
            async {
                let! ct = Async.CancellationToken
                let mutable unregister = new CancellationTokenRegistration()
                let comp = Async.FromContinuations(fun (ccont, econt, tcont) ->
                    this.Subscribe(
                        ?onNext = None,
                        onCompleted = ccont,
                        onError = econt,
                        cancellationToken = ct
                        )
                    unregister <- ct.Register(fun () -> tcont(OperationCanceledException(ct)))
                    )
                try do! comp
                finally
                    unregister.Unregister() |> ignore
                    unregister.Dispose()
            }
        /// Waits for the next element of an IObservable<_> sequence.
        member this.AsyncWaitNext() : Async<ObserverData<'T>> =
            async {
                let! ct = Async.CancellationToken
                return!  Async.FromContinuations(fun (ccont, _, _) ->
                    let mutable sync = obj()
                    let mutable called = false
                    this.Subscribe(
                        onNext = (fun n ->
                            lock sync (fun () ->
                                if not called then
                                    called <- true
                                    n |> ObserverNext |> ccont
                                )
                            ),
                        onCompleted = (fun () ->
                            lock sync (fun () ->
                                if not called then
                                    called <- true
                                    ObserverCompleted |> ccont
                                )
                            ),
                        onError = (fun e ->
                            lock sync (fun () ->
                                if not called then
                                    called <- true
                                    e |> ObserverError |> ccont
                                )
                            ),
                        cancellationToken = ct
                        )
                    )
            }
        member this.Retain quantity =
            let bag = new ConcurrentBag<'T>()
            let returnObservable = new DelegateObservable<'T>(fun obs -> async { () })
            let obs = new DelegateObserver<'T>(fun next ->
                bag.Add next
                )
            this.Subscribe(obs)
            returnObservable

    [<AbstractClass; Sealed; Extension>]
    type Observable =
        static member StartImmediately(del: IObserver<'T> -> Async<unit>) =
            let record = SubjectObservable.Create()
            let observable = record.Observable
            let observer = record.Observer
            del observer |> Async.Start
            observable
        static member Start(del : IObserver<'T> -> Async<unit>) =
            new DelegateObservable<_>(del)

    module Observable =
        let concat first second = ()
        let catch (handler: exn -> unit) (oseq: 'a oseq) = ()
        /// Create a list from an observable sequence. This function will block until the observable has completed.
        let toList (oseq: 'a oseq) = ()
        /// Create an array from an observable sequence. This function will block until the observable has completed.
        let toArray (oseq: 'a oseq) = ()
        /// Create a blocking sequence to enumerate the contents of the observable.
        let toSeq (oseq: 'a oseq) = ()
        /// Create an asynchronous sequence to enumerate the contents of the observable.
        let toAseq (oseq: 'a oseq) = ()

        /// Create an observable to publish each value of a list.
        let ofList (list: 'a list) = ()
        /// Create an observable to publish each value of an array.
        let ofArray (arr: 'a array) = ()
        /// Create an observable to publish each value of a sequence.
        let ofSeq (seq: 'a seq) = ()
        /// Create an observable to publish each value of an async sequence.
        let ofAseq (aseq: 'a aseq) = ()

        /// Connect to a single observable to 
        let connect (connectable: IConnectibleObservable<'a>) = ()
        let publish (oseq: 'a oseq) = ()


    [<AbstractClass; Sealed; Extension>]
    type Linq =
        class
        end

    //[<AbstractClass>]
    //type ObservedCollection private () =
    //    abstract member IsCompletedSuccessfully: bool
    //    abstract member IsCompleted: bool
    //    abstract member IsFaulted: bool
    //    abstract member Error: Exception option

    //    member this.TryGetError(error: exn outref) =
    //        match this.Error with
    //        | Some e ->
    //            error <- e
    //            true
    //        | None ->
    //            false

    ///// Mutable singly linked list, should only be used by the ObservedCollection.
    //[<Sealed; AllowNullLiteral>]
    //type internal Link<'T>(value: 'T) =
    //    let mutable tail : Link<'T> = null
    //    member _.Head = value
    //    member _.Count : int =
    //        match tail with
    //        | null -> 1
    //        | value -> value.Count + 1

    //    member _.TryGetTail() = tail |> Option.ofObj
    //    member _.TryGetTailValue() = tail |> ValueOption.ofObj
    //    member _.TryGetTail(tailLink: Link<'T> outref): bool =
    //        match tail with
    //        | null -> false
    //        | value -> tailLink <- value; true
    //    /// Gets a value for tail which may be null.
    //    member _.UnsafeGetTail() = tail

    //    /// Add a value to the end of the list. This method is not thread-safe.
    //    member _.Append(value : 'T) =
    //        match tail with
    //        | null ->
    //            tail <- new Link<_>(value)
    //            tail
    //        | next ->
    //            next.Append(value)

    //[<Struct>]
    //type internal LinkEnumerator<'T> =
    //    val private head: Link<'T>
    //    val mutable private current: Link<'T> voption

    //    new(first: Link<'T>) = { head = first; current = ValueNone }

    //    member this.Current =
    //        match this.current with
    //        | ValueSome v -> v.Head
    //        | ValueNone -> invalidOp "The enumerator has not progressed to a value."
    //    member this.MoveNext() =
    //        match this.current with
    //        | ValueNone -> this.current <- ValueSome this.head
    //        | ValueSome current -> this.current <- current.TryGetTailValue()
    //        this.current.IsSome

    //    interface IEnumerator<'T> with
    //        member this.Current = this.Current
    //    interface IEnumerator with
    //        member this.Current = this.Current
    //        member this.MoveNext() = this.MoveNext()
    //        member this.Reset() = this.current <- ValueNone
    //    interface IDisposable with
    //        member this.Dispose() = ()

    //type Link<'T> with
    //    member this.GetEnumerator() =
    //        new LinkEnumerator<'T>(this)

    ///// Collection using Link<'T>
    //type internal LinkedList<'T>(head: Link<'T>, tail: Link<'T>) =
    //    let mutable tail = tail

    //    new(head) = LinkedList(head, head)
    //    new(first) = LinkedList(Link(first))

    //    member _.Head = head
    //    member _.GetEnumerator() = head.GetEnumerator()
    //    member _.Append value =
    //        lock head (fun () -> tail <- tail.Append value)

    ///// FIFO collection with async waiting for a value to become available.
    //type internal LinkedQueue<'T> (first: Link<'T>, last: Link<'T>) =
    //    let mutable first = first
    //    let mutable last = last
    //    let hasAny = new ManualResetEvent(false)
    //    let mutable isDisposed = false
    //    let assertNotDisposed () =
    //        match isDisposed with
    //        | true -> raise <| new ObjectDisposedException(nameof LinkedQueue)
    //        | false -> ()

    //    new(head) = LinkedQueue(head, head)
    //    new(first) = LinkedQueue(Link(first))
    //    new() = LinkedQueue(null, null)

    //    member _.Push value =
    //        lock hasAny (fun () ->
    //            match last with
    //            | null ->
    //                first <- Link(value)
    //                last <- first
    //            | link ->
    //                last <- link.Append value
    //                hasAny.Set() |> ignore
    //            )
    //    member _.TryPop() =
    //        assertNotDisposed()
    //        lock hasAny (fun () ->
    //            assertNotDisposed()
    //            match first with
    //            | null -> None
    //            | link ->
    //                match link.UnsafeGetTail() with
    //                | null ->
    //                    first <- null
    //                    last <- null
    //                    hasAny.Reset() |> ignore
    //                | linkTail ->
    //                    first <- linkTail
    //                Some link.Head
    //            )
    //    member this.AsyncPop() : Async<'T> =
    //        let rec tryAsyncPop() =
    //            async {
    //                assertNotDisposed()
    //                do! Async.AwaitWaitHandle hasAny |> Async.Ignore
    //                match this.TryPop() with
    //                | Some value -> return value
    //                | None -> return! tryAsyncPop()
    //            }
    //        tryAsyncPop()
    //    member this.PopAsync(cancellationToken: CancellationToken) : ValueTask<'T> =
    //        match this.TryPop() with
    //        | Some next -> ValueTask<'T>(next)
    //        | None ->
    //            let computation = this.AsyncPop()
    //            let task = Async.StartImmediateAsTask(computation, cancellationToken)
    //            ValueTask<'T>(task)
    //    interface IDisposable with
    //        member _.Dispose() =
    //            if not isDisposed then
    //                lock hasAny (fun () ->
    //                    if not isDisposed then
    //                        isDisposed <- true
    //                        first <- null
    //                        last <- null
    //                        hasAny.Set() |> ignore
    //                        hasAny.Dispose()
    //                    )
    //module ObservedCollection =
    //    type internal Async with
    //        static member GetAsyncWithContinuations() =
    //            let mutable ccont = Unchecked.defaultof<_>
    //            let mutable econt = Unchecked.defaultof<_>
    //            let mutable tcont = Unchecked.defaultof<_>
    //            let async = Async.FromContinuations(
    //                (fun (cc, ec, tc) ->
    //                    ccont <- cc
    //                    econt <- ec
    //                    tcont <- tc
    //                    )
    //                    )
    //            {|
    //                Computation = async
    //                CompleteWith = ccont
    //                FailWith = econt
    //                CancelWith = tcont
    //            |}

    //open ObservedCollection
    //type ObservedCollection<'T> =
    //    /// Observable event that completes or fails
    //    val mutable onCompleted: IObservable<unit>
    //    val mutable internal first: Link<'T>
    //    val mutable internal last: Link<'T>
    //    val mutable internal lastIndex: {| link: Link<'T>; index: int |} voption
    //    val mutable internal length: int
    //    val mutable internal error: exn option
    //    val mutable internal isCompleted: bool
    //    val mutable internal completeWith: unit -> unit
    //    val mutable internal completed: Async<unit>
    //    val mutable internal failWith: exn -> unit
    //    val mutable internal nextReceived: Async<'T>
    //    val mutable internal nextFailed: exn -> unit
    //    val mutable internal onNext: 'T -> unit
    //    new() as this =
    //        let conts = Async.GetAsyncWithContinuations()
    //        let next = Async.GetAsyncWithContinuations()
    //        {
    //            first = null
    //            last = null
    //            lastIndex = ValueNone
    //            length = 0
    //            error = None
    //            isCompleted = false
    //            completed = conts.Computation
    //            completeWith = conts.CompleteWith
    //            failWith = conts.FailWith
    //            onNext = next.CompleteWith
    //            nextFailed = next.FailWith
    //            nextReceived = next.Computation
    //        }

    //    member this.Count =
    //        lock(this.completeWith) (fun () -> this.length)

    //    member this.AsyncWait() = this.completed

    //    member private this.SeekToItemNonblocking(from: Link<'T>, count: int) =
    //        let rec seek (from: Link<'T>) count =
    //            match count with
    //            | 0 -> from
    //            | _ ->
    //                match from.UnsafeGetTail() with
    //                | null -> raise <| new ArgumentOutOfRangeException("index")
    //                | v -> seek v (count - 1)
    //        seek from count
        
    //    member private this.SeekToItemNonblocking(index: int) =
    //        let link =
    //            match this.lastIndex with
    //            | ValueNone -> this.SeekToItemNonblocking(this.first, index)
    //            | ValueSome lastIndex ->
    //                if lastIndex.index < index then this.SeekToItemNonblocking(lastIndex.link, index - lastIndex.index)
    //                else this.SeekToItemNonblocking(this.first, index)
    //        this.lastIndex <- ValueSome {| link = link; index = index |}
    //        link.Head

    //    member private this.AsyncWaitForIndex(index: int) =
    //        async {
    //            while this.length < index do
    //                do! this.waiter |> Async.AwaitWaitHandle |> Async.Ignore
    //        }

    //    /// Indexed access blocks until an item is available at the provided index.
    //    member this.Item
    //        with get(index: int) : 'T =
    //            this.Item(index, false)

    //    member this.Item
    //        with get(index: int, nonblocking: bool) : 'T =
    //            this.GetValue(index, nonblocking)

    //    member this.TryGetValue(index: int) : 'T option =
    //        if this.length > index
    //        then this.SeekToItemNonblocking(index) |> Some
    //        else None

    //    member this.GetValue(index: int, nonblocking: bool) : 'T =
    //        if nonblocking && this.length < index then raise <| new ArgumentOutOfRangeException(nameof index)
    //        if this.length < index then this.AsyncWaitForIndex(index) |> Async.RunSynchronously
    //        this.SeekToItemNonblocking(index)

    //    member this.GetValue(index: int) : 'T =
    //        this.GetValue(index, false)
        
    //    member this.AsyncGetValue(index: int) : Async<'T> =
    //        async {
    //            do! this.AsyncWaitForIndex(index)
    //            return this.SeekToItemNonblocking(index)
    //        }

    //    member this.GetValueAsync(index: int, cancellationToken: CancellationToken) : ValueTask<'T> =
    //        match this.TryGetValue(index) with
    //        | Some v -> ValueTask<'T>(v)
    //        | None ->
    //            let computation = this.AsyncGetValue(index)
    //            let task = Async.StartImmediateAsTask(computation, cancellationToken)
    //            ValueTask<'T>(task)


    //    member this.IsCompletedSuccessfully = lock this.waiter (fun () -> this.completed && this.error |> Option.isNone)
    //    member this.IsCompleted = this.completed || lock this.waiter (fun () -> this.completed)
    //    member this.IsFaulted = this.error |> Option.isSome || lock this.waiter (fun () -> this.error |> Option.isSome)
    //    member this.IsCancelled = false

    //type private ObservedCollectionObserver<'T> (collection: ObservedCollection<'T>) =
    //    interface IObserver<'T> with
    //        member this.OnNext value =
    //            lock collection.waiter (fun () ->
    //                if collection.completed then () else
    //                match collection.last with
    //                | null ->
    //                    collection.last <- Link(value)
    //                    collection.first <- collection.last
    //                | link ->
    //                    collection.last <- link.Append value
    //                collection.length <- collection.length + 1
    //                collection.waiter.Set() |> ignore
    //                collection.waiter.Reset() |> ignore
    //                )
    //        member this.OnError e =
    //            lock collection.waiter (fun () ->
    //                if collection.completed then () else
    //                collection.error <- Some e
    //                collection.completed <- true
    //                collection.waiter.Set() |> ignore
    //                )
    //        member this.OnCompleted() =
    //            lock collection.waiter (fun () ->
    //                if collection.completed then () else
    //                collection.completed <- true
    //                collection.waiter.Set() |> ignore
    //                )

    //[<Struct>]
    //type ObservedCollectionAwaiter<'T>(collection: ObservedCollection<'T>) =
    //    member _.IsCompleted = collection.IsCompleted
    //    member _.GetResult() = ()
    //    interface INotifyCompletion with
    //        member _.OnCompleted(continuation) = ()

    //[<Struct>]
    //type ObservedCollectionBlockingEnumerator<'T> =
    //    struct      
    //    end
    //[<Struct>]
    //type ObservedCollectionNonblockingEnumerator<'T> =
    //    struct
    //    end
    //type ObservedCollectionAsyncEnumerator<'T> =
    //    class
    //    end