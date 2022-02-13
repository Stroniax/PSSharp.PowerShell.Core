namespace PSSharp
open System.Threading.Tasks

module TaskLike =
    [<return: Struct>]
    let (|IsTask|_|) (obj : obj) =
        match obj with
        | :? Task as t -> ValueSome t
        | _ -> ValueNone
    let (|IsTaskLike|NotTaskLike|) obj =
        match DynamicTaskExecutor.IsTaskLike(obj) with
        | true -> IsTaskLike
        | false -> NotTaskLike
    [<return: Struct>]
    let (|IsValueTask|_|) (obj : obj) =
        match obj with
        | :? ValueTask as vt -> ValueSome vt
        | _ -> ValueNone
    [<return: Struct>]
    let (|IsValueTaskOf|_|) (obj : obj) =
        match obj with
        | null -> ValueNone
        | vt when 
            let t = vt.GetType()
            t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<ValueTask<_>>
            ->
                ValueSome (vt :?> ValueTask<_>)
        | _ -> ValueNone
    [<return: Struct>]
    let (|IsTaskLikeOf|_|) obj =
        match DynamicTaskExecutor.IsTaskLikeOf(obj) with
        | true, t -> ValueSome t
        | false, _ -> ValueNone
    /// Returns a task representation of a task-like instance.
    [<CompiledName("AsTask")>]
    let asTask (taskLike : obj) : Task =
        match taskLike with
        | IsTask t -> t
        | IsValueTask vt -> vt.AsTask()
        | IsValueTaskOf vt -> vt.AsTask()
        | _ -> DynamicTaskExecutor.RunAsTask(taskLike)
    /// Returns ValueSome with a task representation of a task-like instance, or ValueNone.
    [<CompiledName("TryAsTask")>]
    let tryAsTask (taskLike : obj) : Task voption =
        match taskLike with
        | IsTask t -> t |> ValueSome
        | IsValueTask vt -> vt.AsTask() |> ValueSome
        | vt when
            let t = vt.GetType()
            t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<ValueTask<_>>
            -> DynamicTaskExecutor.GetValueTaskAsTask(vt) |> ValueSome
        | IsTaskLike -> DynamicTaskExecutor.RunAsTask(taskLike) |> ValueSome
        | _ -> ValueNone
    /// Creates a computation to await a task. If the task is Task<T>, the result T is returned
    /// as ValueSome (T :> obj). Otherwise, ValueNone is returned.
    let awaitTaskOrTaskOf (task : Task) =
        async {
            match DynamicTaskExecutor.IsTaskLikeOf(task) with
            | true, _ ->
                let! taskResult = DynamicTaskExecutor.RunAsTaskOf(task) |> Async.AwaitTask
                return ValueSome taskResult
            | false, _ ->
                do! task |> Async.AwaitTask
                return ValueNone
        }