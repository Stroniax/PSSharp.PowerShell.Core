namespace PSSharp;

/// <summary>
/// A job that completes when an awaitable object is completed. "Awaitable object" is used to refer
/// to any type which implements the task-based asynchronous pattern by defining a public GetAwaiter()
/// method that returns an object with a GetResult() method and IsCompleted property.
/// </summary>
public class AwaiterChildJob : Job
{
    #region instance members
    private readonly string? _location;
    /// <summary>
    /// Action to stop the <see langword="async"/> operation, if provided.
    /// </summary>
    private readonly Action? _cancellation;
    /// <summary>
    /// <see langword="true"/> if the underlying operation can be cancelled when stopping the job.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_cancellation))]
    public bool SupportsCancellation { get => _cancellation is not null; }
    /// <summary>
    /// <see langword="true"/> if the job has data that has not been read.
    /// </summary>
    public override bool HasMoreData => Output.Count > 0 || Error.Count > 0;
    /// <summary>
    /// Location at which the job is running. Defaults to <see cref="Environment.MachineName"/> unless
    /// specified in the call to <see cref="StartJob(object, Action?, string?, string?, string?)"/>.
    /// </summary>
    public override string Location => _location ?? Environment.MachineName;
    /// <summary>
    /// Status of the job, which is <see cref="string.Empty"/>.
    /// </summary>
    public override string StatusMessage => string.Empty;

    /// <summary>
    /// Sends a cancellation request to the operation, if possible.
    /// Otherwise, sets the job state to <see cref="JobState.Stopped"/>
    /// and stops waiting for the operation.
    /// </summary>
    public override void StopJob()
    {
        if (SupportsCancellation && JobStateInfo.State == JobState.Running)
        {
            lock (_cancellation)
            {
                if (JobStateInfo.State == JobState.Running)
                {
                    SetJobState(JobState.Stopping);
                    _cancellation();
                }
            }
        }
        if (!SupportsCancellation && JobStateInfo.State == JobState.Running)
        {
            Verbose.Add(new(UnstoppableAwaitJob));
            SetJobState(JobState.Stopped);
        }
    }
    #endregion instance members

    #region private default await
    /// <summary>
    /// Somehow indicates to the awaitable object how to proceed after completion in
    /// a way that will conclude the current job.
    /// </summary>
    /// <param name="awaitable">An object that the job should await.</param>
    /// <exception cref="ArgumentException">The object is not awaitable.</exception>
    protected virtual void RegisterContinuation(object awaitable)
    {
        if (DynamicTaskExecutor.IsTaskLikeOf(awaitable, out _))
        {
            AwaitWithResult(awaitable);
        }
        else if (DynamicTaskExecutor.IsTaskLike(awaitable))
        {
            AwaitWithoutResult(awaitable);
        }
        else
        {
            throw new ArgumentException(NotAwaitable, nameof(awaitable));
        }
    }
    /// <summary>
    /// Asynchronously awaits the object, not expecting a result.
    /// </summary>
    /// <param name="awaitable">The awaitable instance.</param>
    private async void AwaitWithoutResult(dynamic awaitable)
    {
        await Task.Yield();
        try
        {
            await awaitable;
            SetJobState(JobState.Completed);
        }
        catch (Exception e)
        {
            FailWith(e, awaitable);
        }
    }
    /// <summary>
    /// Asynchronously awaits the object, expecting a result.
    /// </summary>
    /// <param name="awaitable">The awaitable instance.</param>
    private async void AwaitWithResult(dynamic awaitable)
    {
        await Task.Yield();
        try
        {
            var result = await awaitable;
            Output.Add(PSObject.AsPSObject(result));
            SetJobState(JobState.Completed);
        }
        catch (Exception e)
        {
            FailWith(e, awaitable);
        }
    }
    /// <summary>
    /// Fail with the exception, or set the job state to <see cref="JobState.Stopped"/> if it is currently
    /// <see cref="JobState.Stopping"/>.
    /// </summary>
    /// <param name="e">The exception that caused the job to fail.</param>
    /// <param name="targetObject">The target object of the error.</param>
    private void FailWith(Exception e, object? targetObject)
    {
        if (JobStateInfo.State == JobState.Stopping)
        {
            SetJobState(JobState.Stopped);
        }
        else
        {
            Error.Add(new ErrorRecord(
                e,
                nameof(AwaitableJobError),
                ErrorCategory.NotSpecified,
                targetObject
                ));
            SetJobState(JobState.Failed);
        }
    }
    #endregion private default await

    #region start job
    /// <summary>
    /// Construct a new <see cref="AwaiterChildJob"/> and start it.
    /// </summary>
    /// <param name="awaitable"></param>
    /// <param name="cancellation"></param>
    /// <param name="name"></param>
    /// <param name="location"></param>
    /// <param name="command"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    internal AwaiterChildJob(object awaitable, Action? cancellation, string? name, string? location, string? command)
        : base(command, name)
    {
        PSJobTypeName = nameof(AwaiterChildJob);
        _location = location;
        _cancellation = cancellation;

        if (awaitable is null) throw new ArgumentNullException(nameof(awaitable));
        SetJobState(JobState.Running);
        RegisterContinuation(awaitable);
    }

    /// <summary>
    /// Determines if a <see cref="Task"/> is actually a <see cref="Task{T}"/>; if it is,
    /// <paramref name="startJob"/> will return a constructor method for <see cref="TaskChildJob{T}"/>.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <param name="startJob">A constructor for the generic version of the <see cref="TaskChildJob{T}"/>.</param>
    /// <returns><see langword="true"/> if the task is <see cref="Task{TResult}"/>.</returns>
    private static bool IsTaskOf(Task task, [MaybeNullWhen(false)] out Func<Task, Action?, string?, string?, string?, TaskChildJob> startJob)
    {
        var awaiterType = task.GetType().GetMethod(nameof(Task.GetAwaiter), DynamicTaskExecutor.InstanceMemberBindingFlags, Type.EmptyTypes)?.ReturnType;
        if (awaiterType is null
            || !awaiterType.IsGenericType)
        {
            startJob = null;
            return false;
        }
        var returnType = awaiterType.GetGenericArguments()[0];
        var taskJobType = typeof(TaskChildJob<>).MakeGenericType(returnType);
        var ctor = taskJobType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
        startJob = (task, cancellation, jobName, location, command) =>
        {
            return (TaskChildJob)ctor.Invoke(new object?[] {
                task,
                cancellation,
                jobName,
                location,
                command
            });
        };
        return true;
    }

    /// <summary>
    /// Creates a job to represent an <see langword="await"/> operation. The job will complete
    /// with the result of the operation, if any.
    /// </summary>
    /// <param name="awaitable">Must be a task-like type. The task-like members must be defined by the type,
    /// not provided by extension methods.</param>
    /// <param name="cancellation">An action that can cancel the operation, if any.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="location">The location at which the job is running.</param>
    /// <param name="command">The PowerShell command that created the job or that the job is executing.</param>
    /// <returns>A running awaiter job representing the async operation.</returns>
    public static AwaiterChildJob StartJob(
        object awaitable,
        Action? cancellation = null,
        string? jobName = null,
        string? location = null,
        string? command = null
        )
    {
        if (awaitable is Task task)
        {
            return StartJob(task, cancellation, jobName, location, command);
        }
        else
        {
            return new(awaitable, cancellation, jobName, location, command);
        }
    }

    /// <summary>
    /// Creates a job to represent an <see langword="await"/> operation. The job will complete
    /// with the result of the operation, if any.
    /// </summary>
    /// <param name="awaitable">Must be a task-like type. The task-like members must be defined by the type,
    /// not provided by extension methods.</param>
    /// <param name="cancellationTokenSource">A source that can be used to cancel the operation.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="location">The location at which the job is running.</param>
    /// <param name="command">The PowerShell command that created the job or that the job is executing.</param>
    /// <returns>A running awaiter job representing the async operation.</returns>
    public static AwaiterChildJob StartJob(
        object awaitable,
        CancellationTokenSource cancellationTokenSource,
        string? jobName = null,
        string? location = null,
        string? command = null
        ) => StartJob(awaitable, () => cancellationTokenSource.Cancel(), jobName, location, command);

    /// <summary>
    /// Starts a task job from a task.
    /// </summary>
    /// <param name="task">The task that the job represents.</param>
    /// <param name="cancellation">An action that can cancel the task.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="location">The location at which the job is running.</param>
    /// /// <param name="command">The PowerShell command that created the job or that the job is executing.</param>
    /// <returns>A running task job representing the task.</returns>
    public static TaskChildJob StartJob(
        Task task,
        Action? cancellation = null,
        string? jobName = null,
        string? location = null,
        string? command = null)
    {
        if (TaskChildJob.IsTaskOf(task, out var startJob))
        {
            return startJob(
                task,
                cancellation,
                jobName,
                location,
                command);
        }
        else
        {
            return new TaskChildJob(
                task,
                cancellation,
                jobName,
                location,
                command);
        }
    }
    /// <inheritdoc cref="StartJob(Task, Action?, string?, string?, string?)"/>
    public static TaskChildJob<T> StartJob<T>(
        Task<T> task,
        Action? cancellation = null,
        string? jobName = null,
        string? location = null,
        string? command = null)
    {
        return new TaskChildJob<T>(
            task,
            cancellation,
            jobName,
            location,
            command
            );
    }
    #endregion start job
}
