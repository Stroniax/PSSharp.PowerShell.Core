namespace PSSharp;
using System.Runtime.CompilerServices;

/// <summary>
/// Contains helper methods that may be called from F# which utilize the C# dynamic keyword to await
/// task-like instances.
/// </summary>
internal static class DynamicTaskExecutor
{
    /// <summary>
    /// Binding flags to pass when getting a public instance member of a type.
    /// </summary>
    internal const BindingFlags InstanceMemberBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
    /// <summary>
    /// Determines if an object is task-like.
    /// </summary>
    /// <param name="obj">The object that may be task-like.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is task-like.</returns>
    public static bool IsTaskLike(object? obj) => IsTaskLike(obj, out _);
    /// <summary>
    /// Determines if an object is task-like.
    /// </summary>
    /// <param name="obj">The object that may be task-like.</param>
    /// <param name="getResult">The GetResult method of the type returned by GetAwaiter.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is task-like.</returns>
    private static bool IsTaskLike(object? obj, [MaybeNullWhen(false)] out MethodInfo getResult)
    {
        getResult = null;
        if (obj is null) return false;
        var type = obj.GetType();
        var getAwaiter = type.GetMethod(nameof(Task.GetAwaiter), InstanceMemberBindingFlags, Type.EmptyTypes);
        if (getAwaiter is null) return false;
        getResult = getAwaiter.ReturnType.GetMethod(nameof(TaskAwaiter.GetResult), InstanceMemberBindingFlags, Type.EmptyTypes);
        return getResult is not null;
    }
    /// <summary>
    /// Determines if an object is task-like and returns a value.
    /// </summary>
    /// <param name="obj">The object that may be task-like.</param>
    /// <param name="returnType">The return type of the task-like value.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is task-like and will return a value when awaited.</returns>
    public static bool IsTaskLikeOf(object obj, [MaybeNullWhen(false)] out Type returnType)
    {
        returnType = null;
        if (IsTaskLike(obj, out var getResult)
            && getResult.ReturnType != typeof(void))
        {
            returnType = getResult.ReturnType;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Returns a <see cref="Task"/> (or <see cref="Task{T}"/>) that will complete when the
    /// <paramref name="taskLike"/> value completes.
    /// </summary>
    /// <param name="taskLike">An awaitable object.</param>
    /// <returns></returns>
    public static Task RunAsTask(object taskLike)
    {
        if (IsTaskLikeOf(taskLike, out _))
        {
            return RunAsTaskOf(taskLike);
        }
        else
        {
            return RunAsTaskInner(taskLike);
        }
    }
    /// <summary>
    /// Waits for a task-like instance to complete.
    /// </summary>
    /// <param name="taskLike"></param>
    /// <returns></returns>
    private static async Task RunAsTaskInner(dynamic taskLike)
    {
        await taskLike;
    }
    /// <summary>
    /// Waits for a task-like instance to complete and returns the result.
    /// </summary>
    /// <param name="taskLike">An awaitable object that will return a value.</param>
    /// <returns></returns>
    public static async Task<object> RunAsTaskOf(dynamic taskLike)
    {
        return await taskLike;
    }
    /// <summary>
    /// Waits for a task-like instance to complete and returns the strongly typed result.
    /// </summary>
    /// <param name="taskLike">An awaitable object that will return an instance of type <typeparamref name="T"/>.</param>
    /// <returns></returns>
    public static async Task<T> RunAsTaskOf<T>(dynamic taskLike)
    {
        return await taskLike;
    }
    /// <summary>
    /// Calls <paramref name="action"/> after awaiting <paramref name="taskLike"/>.
    /// </summary>
    /// <param name="taskLike"></param>
    /// <param name="action"></param>
    public static async void Subscribe(dynamic taskLike, Action action)
    {
        await taskLike;
        action();
    }
    /// <summary>
    /// Calls <paramref name="action"/> with the result obtained after awaiting <paramref name="taskLike"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="taskLike"></param>
    /// <param name="action"></param>
    public static async void Subscribe<T>(dynamic taskLike, Action<T> action)
    {
        var result = await taskLike;
        action(result);
    }
    /// <summary>
    /// Calls <see cref="ValueTask.AsTask"/> or <see cref="ValueTask{TResult}.AsTask"/>
    /// on <paramref name="valueTask"/>.
    /// </summary>
    /// <param name="valueTask">A <see cref="ValueTask"/> or <see cref="ValueTask{TResult}"/> from which to
    /// get the task.</param>
    /// <returns>The task version of the value task.</returns>
    public static Task GetValueTaskAsTask(dynamic valueTask)
        => valueTask.AsTask();
}
