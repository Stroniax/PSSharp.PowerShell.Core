namespace PSSharp

/// Encompasses a suite of functionality and structure for functional style enumeration, such as the
/// IAsyncIterator<'T> and related types.
module FunctionalEnumeration =
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Threading
    open System.Threading.Tasks

    /// Identifies the value assigned to a WatchedValue<_>.
    type CurrentWatchedValue<'T> =
        {
            /// The value at the time this record was constructed.
            Current: 'T
            /// A token that is cancelled when a new value is published.
            Replaced: CancellationToken
        }
    
    /// Identifies the previous and newly assigned value of a WatchedValue<_> when the value
    /// is modified.
    type ReplacedWatchedValue<'T> =
        {
            /// The value prior to the replacement.
            Previous: 'T
            /// The newly assigned value.
            Replacement: CurrentWatchedValue<'T>
        }

    /// Event handler triggered when a watched value gets changed.
    type WatchedValueChangedEventHandler<'T> = delegate of sender: obj * args: ReplacedWatchedValue<'T> -> unit

    /// Exposes a mutable value that notifies subscribers when it is modified.
    [<Sealed>]
    type WatchedValue<'T>(current: 'T) =
        let mutable _current = current;
        let mutable _changedCancellation = new CancellationTokenSource()
        let mutable _isDisposed = false
        let _changedEvent = new Event<WatchedValueChangedEventHandler<'T>, _>()
        let throwIfDisposed() = if _isDisposed then raise <| new ObjectDisposedException(nameof WatchedValue)
        let getCurrent() =
            lock _changedEvent (fun () ->
                {
                    Current = _current
                    Replaced = if _isDisposed then CancellationToken(false) else _changedCancellation.Token
                }
                )
        let getReplacement prev =
            lock _changedEvent (fun () ->
                {
                    Previous = prev
                    Replacement = getCurrent()
                }
                )

        [<CLIEvent>]
        member this.Changed =
            throwIfDisposed()
            lock _changedEvent (fun () ->
                throwIfDisposed()
                _changedEvent.Publish
                )

        member this.GetValue() = getCurrent()
        member this.SetValue newValue =
            throwIfDisposed()
            let replacementInfo = lock _changedEvent (fun () ->
                throwIfDisposed()
                let last = _current
                _changedCancellation.Cancel()
                _changedCancellation.Dispose()
                _changedCancellation <- new CancellationTokenSource()
                _current <- newValue
                getReplacement last
                )
            if (not replacementInfo.Replacement.Replaced.IsCancellationRequested) || _isDisposed then
                _changedEvent.Trigger (this, replacementInfo)
            replacementInfo

        member this.Dispose() =
            // Only dispose once. Do not call cancellation.Dispose inside of lock
            // in case it is a time consuming process.
            if not _isDisposed then
                let nowDisposed = lock _changedEvent (fun () ->
                    if not _isDisposed then
                        _isDisposed <- true
                        true
                    else false
                    )
                if nowDisposed then
                    _changedCancellation.Dispose()
        interface IDisposable with member this.Dispose() = this.Dispose()
    
    type IAsyncIterator<'T> =
        /// Gets the value of the current iteration, if any is available.
        abstract TryGetCurrent: unit -> 'T option
        /// Asynchronously gets the next iteration value.
        abstract TryGetNext: unit -> Async<IAsyncIterator<'T> option>
    type IPreemptiveAsyncIterator<'T> =
        inherit IAsyncIterator<'T>
        /// Gets the next iterator value if no waiting is required to retreive the value.
        abstract TryGetNextImmediately: unit -> IPreemptiveAsyncIterator<'T> option
        /// The number of available values that can be iterated to.
        abstract AvailableCount: int

    /// Represents a value which can be enumerated synchronously or asynchronously, particularly designed for
    /// asynchronous functional programming.
    [<RequireQualifiedAccess>]
    type AsyncIterator<'T> =
    /// The current iteration represents a point before the start of the sequence. No value is available,
    /// but the sequence has not necessarily ended.
    | IteratorStart of getFirst: (unit -> Async<AsyncIterator<'T>>)
    /// The current iteration represents a value and is not necessarily the end of the sequence.
    | IteratorAvailable of current: 'T * getNext: (unit -> Async<AsyncIterator<'T>>)
    /// The current iteration is at the end of the sequence, and has a value. No values remain beyond
    /// this value.
    | IteratorEnd of last: 'T
    /// The current iteration is a wrapper for some other implementation of the IAsyncIterator<'T> type.
    | BoxedIterator of boxed: IAsyncIterator<'T>
    /// The current iteration represents a point beyond the end of the sequence. No value is available,
    /// and no values remain.
    | IteratorPastEnd
        member this.GetNext() =
            async {
                match this with
                | IteratorStart fn ->
                    let! next = fn()
                    return next
                | IteratorAvailable (_, fn) ->
                    let! next = fn()
                    return next
                | _ -> return IteratorPastEnd
            }
        member this.TryGetCurrent() =
            match this with
            | IteratorAvailable (value, _) -> Some value
            | IteratorEnd value -> Some value
            | _ -> None
        member this.TryGetCurrentValue() =
            match this with
            | IteratorAvailable (value, _) -> ValueSome value
            | IteratorEnd value -> ValueSome value
            | _ -> ValueNone
        member this.GetCurrent() =
            match this with
            | IteratorAvailable (value, _) ->  value
            | IteratorEnd value -> value
            | _ -> invalidOp "Iterator has no value."
        member this.TryGetNext() =
            async {
                match this with
                | IteratorStart fn ->
                    let! next = fn()
                    return Some next
                | IteratorAvailable (_, fn) ->
                    let! next = fn()
                    return Some next
                | _ -> return None
            }
        member this.HasCurrent  =
            match this with
            | IteratorAvailable _ -> true
            | IteratorEnd _ -> true
            | _ -> false
        member this.HasNext =
            match this with
            | IteratorStart _ -> true
            | IteratorAvailable _ -> true
            | _ -> false
    
        member this.GetEnumerator() = new AsyncIteratorSynchronousEnumerator<'T>(this)
        member this.GetAsyncEnumerator(cancellationToken) = new AsyncIteratorAsyncEnumerator<'T>(this, cancellationToken)
        interface IEnumerable<'T> with
            member this.GetEnumerator() = this.GetEnumerator()
        interface IEnumerable with
            member this.GetEnumerator() = this.GetEnumerator()
        interface IAsyncEnumerable<'T> with
            member this.GetAsyncEnumerator(ct) = this.GetAsyncEnumerator(ct)
        interface IAsyncIterator<'T> with
            member this.TryGetCurrent() = this.TryGetCurrent()
            member this.TryGetNext() =
                // Have to type the Async<Option<>> as the interface type
                async {
                    let! next = this.TryGetNext()
                    return match next with
                    | Some next -> Some next
                    | None -> None
                }
        interface IPreemptiveAsyncIterator<'T> with
            member this.AvailableCount =
                match this with
                | BoxedIterator inner ->
                    match inner with
                    | :? IPreemptiveAsyncIterator<'T> as it -> it.AvailableCount
                    | _ -> 0
                | _ -> 0
            member this.TryGetNextImmediately() =
                match this with
                | BoxedIterator inner ->
                    match inner with
                    | :? IPreemptiveAsyncIterator<'T> as it -> it.TryGetNextImmediately()
                    | _ -> None
                | _ -> None

    and [<Struct>] AsyncIteratorSynchronousEnumerator<'T> =
        val mutable private _iter: AsyncIterator<'T>
        new(iter: AsyncIterator<'T>) =
            {
                _iter = iter
            }

        member this.MoveNext() =
            this._iter <- this._iter.GetNext() |> Async.RunSynchronously
            this._iter.HasCurrent
        member this.Current =
            match this._iter.TryGetCurrent() with
            | Some v -> v
            | None -> invalidOp "Iterator has no value."
        interface IEnumerator<'T> with
            member this.Current = this.Current
        interface IEnumerator with
            member this.MoveNext() = this.MoveNext()
            member this.Current = this.Current
            member this.Reset() = ()
        interface IDisposable with
            member this.Dispose() = ()
    and AsyncIteratorAsyncEnumerator<'T>(iter: AsyncIterator<'T>, cancellationToken: CancellationToken) =
        let mutable iter = iter
        member this.Current = iter.GetCurrent()
        member this.MoveNextAsync() =
            let comp = 
                async {
                    let! next = iter.GetNext()
                    iter <- next
                    return iter.HasCurrent
                }
            let task = Async.StartImmediateAsTask(comp, cancellationToken)
            new ValueTask<bool>(task)
        interface IAsyncEnumerator<'T> with
            member this.Current = this.Current
            member this.MoveNextAsync() = this.MoveNextAsync()
        interface IAsyncDisposable with
            member this.DisposeAsync() = ValueTask.CompletedTask

    type 'a aseq = IAsyncEnumerable<'a>
    type 'a iter = AsyncIterator<'a>

    //module AsyncIterator =
    //    let map (fn: 'a iter -> 'b) (iter: 'a iter): 'b iter = ()
    //    let asyncMap (fn: 'a iter -> Async<'b>) (iter: 'a iter): 'b iter = ()
    //    let iter (fn: 'a iter -> unit) (iter: 'a iter): Async<unit> = ()
    //    let asyncIter (fn: 'a iter -> Async<unit>) (iter: 'a iter): Async<unit> = ()
    //    let enumerate iter = ()
    //    let asyncEnumerate iter = ()
        
    //    let take (quantity: int) (iter: 'a iter): Async<'a iter option> = ()
    //    let skip (quantity: int) (iter: 'a iter): Async<'a iter option> = ()
    //    let count (iter: 'a iter): Async<int> = ()



    //    /// Loads up to lead number of iterations in the background prior to retrieval.
    //    let lead (lead: int) (iter: 'a iter): 'a iter = ()
    //    /// Loads iterations in the background until a predicate is satisfied.
    //    let leadUntil (until: 'a iter -> bool) (iter: 'a iter): IPerceptiveAsyncIterator<'T> = ()
    //    /// Loads all elements in the background.
    //    let leadAll (iter: 'a iter): IPerceptiveAsyncIterator<'T> = iter |> loadUntil (fun _ -> true)
        
    //    /// Creates an IEnumerable<'T> representation of an IAsyncIterator<'T>. The sequence will block upon
    //    /// enumeration until each value becomes available.
    //    let toSeq (iter: 'a iter): 'a seq = ()
    //    /// Creates an IAsyncIterator<'T> representation of an IEnumerable<'T>.
    //    let ofSeq (seq: 'a seq): 'a iter = ()
    //    /// Creates an IAsyncEnumerable<'T> value that enumerations all iterable values in an IAsyncIterator<'T>.
    //    let toAseq (iter: 'a iter): 'a aseq = ()
    //    /// Creates an IAsyncIterator<'T> that iterates through all values of an IAsyncEnumerable<'T> collection.
    //    let ofAseq (aseq: 'a aseq): 'a iter = ()
    //    /// Creates an immutable FSharp List<'T> representation of an IAsyncIterator<'T>. This method will block
    //    /// until the iterable sequence is completed.
    //    let toList (iter: 'a iter): 'a list = ()
    //    /// Creates an IAsyncIterator<'T> that iterates through all values of an immutable FSharp List<'T>.
    //    let ofList (list: 'a list): 'a iter = ()
    //    /// Creates an array representation of an IAsyncIterator<'T>. This method will block until the iterable
    //    /// sequence is completed.
    //    let toArray (iter: 'a iter): 'a array = ()
    //    /// Creates an IAsyncIterator<'T> that iterates through all values of an array.
    //    let ofArray (arr: 'a array): 'a iter = ()

