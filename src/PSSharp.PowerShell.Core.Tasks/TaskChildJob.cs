using System.Management.Automation.Internal;

namespace PSSharp;

/// <summary>
/// A job that waits for a <see cref="Task"/> or <see cref="Task{T}"/> and returns the result, if any.
/// </summary>
public class TaskChildJob
    : AwaiterChildJob
{
    /// <summary>
    /// Task that this job awaits.
    /// </summary>
    private readonly WeakReference<Task> _task;
    /// <summary>
    /// Status of the task.
    /// </summary>
    public override string StatusMessage => _task.TryGetTarget(out var task)
        ? task.Status.ToString()
        : string.Empty;
    /// <summary>
    /// Called by the continuation to the task when it has completed.
    /// </summary>
    /// <param name="task">The task that just completed.</param>.
    private async void OnTaskCompleted(Task task)
    {
        if (task.IsCanceled)
        {
            SetJobState(JobState.Stopped);
        }
        else if (task.IsFaulted)
        {
            var e = task.Exception?.Flatten().GetBaseException()
                ?? new Exception(AwaitableJobError);
            var er = new ErrorRecord(
                e,
                nameof(AwaitableJobError),
                ErrorCategory.NotSpecified,
                task);
            Error.Add(er);
            SetJobState(JobState.Failed);
        }
        else // if (task.IsCompletedSuccessfully)
        {
            await AddResultToOutput(task);
            SetJobState(JobState.Completed);
        }
    }
    /// <summary>
    /// Method allowing derived classes to add <see cref="Task{T}.Result"/> to <see cref="Job.Output"/>.
    /// </summary>
    /// <param name="task">The successfully completed task.</param>
    protected virtual ValueTask AddResultToOutput(Task task) => ValueTask.CompletedTask;

    internal TaskChildJob(
        Task task,
        Action? cancellation,
        string? jobName,
        string? location,
        string? command)
        : base(
            task ?? throw new ArgumentNullException(nameof(task)),
            cancellation,
            jobName,
            location,
            command
            )
    {
        PSJobTypeName = nameof(TaskChildJob);
        _task = new(task ?? throw new ArgumentNullException(nameof(task)));
    }

    /// <summary>
    /// Registers task continuation to the <paramref name="awaitable"/> that is a <see cref="Task"/>.
    /// </summary>
    /// <param name="awaitable"></param>
    protected override void RegisterContinuation(object awaitable)
    {
        var task = (Task)awaitable;
        task.ContinueWith(OnTaskCompleted);
    }
}

/// <summary>
/// A job that waits for a <see cref="Task{T}"/> and returns the result, if any.
/// </summary>
/// <typeparam name="T">The result of the task.</typeparam>
public sealed class TaskChildJob<T>
    : TaskChildJob
{
    internal TaskChildJob(
        Task<T> task,
        Action? cancellation,
        string? jobName,
        string? location,
        string? command
        )
        : base(
            task,
            cancellation,
            jobName,
            location,
            command)
    {
    }
    /// <summary>
    /// Adds the value of <see cref="Task{T}.Result"/> to the job output.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    protected override async ValueTask AddResultToOutput(Task task)
    {
        var taskOf = (Task<T>)task;
        PSObject? pso;
        try
        {
            // Attempt to read Task.Result sometimes fails (like VoidTaskResult)
            // so we will catch and set to null if that is the case, that way we
            // aren't adding output when we shouldn't.
            var result = await taskOf;
            pso = result is null
                ? AutomationNull.Value
                : PSObject.AsPSObject(result);
        }
        catch (Exception e)
        {
            Debug.Add(new(
                string.Format(ExpectedTaskAwaitResultInterpolated, $"\n{e}")
                ));
            pso = null;
        }
        if (pso is not null)
        {
            Output.Add(pso);
        }
        await base.AddResultToOutput(task);
    }
}