namespace PSSharp;

using System.Collections.ObjectModel;
using static PSSharp.JobStatus;

/// <summary>
/// PowerShell Job to handle a C# <see langword="async"/>/<see langword="await"/> operation.
/// While the <see cref="AwaiterJob"/> will generally refer to a single operation, it is possible
/// that the job may refer to multiple async operations created as the result of a single command.
/// In the latter case, a child job will refer to each individual operation.
/// </summary>
public sealed class AwaiterJob
    : Job
{
    private static bool IsFinished(JobState state)
        => state == JobState.Completed
        || state == JobState.Failed
        || state == JobState.Stopped;
#if DEBUG
    /// <summary>
    /// The number of child jobs that the current job is waiting on.
    /// </summary>
    public int RunningJobCount => _runningJobs.Count;
#endif

    /// <summary>
    /// The number of child jobs that are currently running an not completed.
    /// </summary>
    private readonly Collection<AwaiterChildJob> _runningJobs;
    /// <summary>
    /// Status message from <see cref="JobStatus"/>.
    /// </summary>
    private string _statusMessage = JobStatus.Processing;
    /// <summary>
    /// <see langword="true"/> if any child jobs have unread data.
    /// </summary>
    public override bool HasMoreData => ChildJobs.Any(c => c.HasMoreData);
    /// <summary>
    /// Location at which the job is running. A comma-separated list of the <see cref="Job.Location"/>
    /// of each child job.
    /// </summary>
    public override string Location => string.Join(", ", ChildJobs.Select(i => i.Location).Distinct(StringComparer.OrdinalIgnoreCase));
    /// <summary>
    /// The status of the job.
    /// </summary>
    public override string StatusMessage => _statusMessage;
    /// <summary>
    /// Stops the job.
    /// </summary>
    public override void StopJob()
    {
        SetJobState(JobState.Stopping);
    }
    /// <summary>
    /// Subscribed to child jobs to track child job completion.
    /// </summary>
    /// <param name="sender">The child job.</param>
    /// <param name="e">The job state info of the child job.</param>
    private void OnChildJobStateChanged(object? sender, JobStateEventArgs e)
    {
        if (sender is AwaiterChildJob job)
        {
            var finished = IsFinished(e.JobStateInfo.State);
            if (finished)
            {
                job.StateChanged -= OnChildJobStateChanged;
            }

            lock (_runningJobs)
            {
                var removed = _runningJobs.Remove(job);
                if (_runningJobs.Count == 0)
                {
                    SetJobState(GetTerminalJobStateFromChildJobs());
                }
            }
        }
    }

    private AwaiterJob(
        string? jobName,
        string? command,
        IEnumerable<AwaiterChildJob> childJobs
        )
        : base(command, jobName)
    {
        PSJobTypeName = nameof(AwaiterJob);
        _runningJobs = new();
        SetJobState(JobState.Running);
        lock (_runningJobs)
        {
            foreach (var childJob in childJobs)
            {
                _runningJobs.Add(childJob);
                ChildJobs.Add(childJob);
                childJob.StateChanged += OnChildJobStateChanged;
                // run with current job state to clear the job if it is already finished
                OnChildJobStateChanged(childJob, new(childJob.JobStateInfo));
            }

            if (_runningJobs.Count == 0)
            {
                SetJobState(GetTerminalJobStateFromChildJobs());
            }
        }
    }

    /// <summary>
    /// Starts an <see cref="AwaiterJob"/> as a wrapper around all child jobs provided.
    /// </summary>
    /// <param name="childJobs">The child jobs that the new job will await.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="command">The command that the job is executing.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"><paramref name="childJobs"/> is null.</exception>
    public static AwaiterJob StartJob(
        IEnumerable<AwaiterChildJob> childJobs,
        string? jobName = null,
        string? command = null)
    {
        return new AwaiterJob(jobName, command, childJobs);
    }
    /// <summary>
    /// Starts an <see cref="AwaiterJob"/> as a wrapper around <paramref name="childJob"/>.
    /// </summary>
    /// <param name="childJob">The child job that the new job will await.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="command">The command that the job is executing.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"><paramref name="childJob"/> is null.</exception>
    public static AwaiterJob StartJob(
        AwaiterChildJob childJob,
        string? jobName = null,
        string? command = null
        )
    {
        return new AwaiterJob(
            jobName,
            command,
            new[] {
                childJob ?? throw new ArgumentNullException(nameof(childJob))
            }
            );
    }


    private JobState GetTerminalJobStateFromChildJobs()
    {
        // Failed > Stopped > Completed
        bool stopped = false;
        foreach (var job in ChildJobs)
        {
            if (job.JobStateInfo.State == JobState.Failed)
            {
                return JobState.Failed;
            }
            else if (job.JobStateInfo.State == JobState.Stopped)
            {
                stopped = true;
            }
        }
        return stopped ? JobState.Stopped : JobState.Completed;
    }
}
