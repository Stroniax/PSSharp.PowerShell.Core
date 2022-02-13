namespace PSSharp
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Management.Automation
    open System.Linq


    module JobExtensions =
        /// Default job.Location
        let internal GetDefaultJobLocation (job : Job) = Environment.MachineName
        /// Function to test if job.HasMoreData is true. Specifically for testing if any child job
        /// has more data in a job.
        let private TestJobHasMoreData (job : Job) = job.HasMoreData
        /// Default implementation for job.HasMoreData
        let internal GetDefaultHasMoreData (job : Job) =
            job.Output.Count > 0
            || job.Error.Count > 0
            || job.Warning.Count > 0
            || job.Verbose.Count > 0
            || job.Debug.Count > 0
            || job.Information.Count > 0
            || job.Progress.Count > 0
            || job.ChildJobs.Any(TestJobHasMoreData)
        let internal GetDefaultStatusMessage (job : Job) = String.Empty;

        type Job with
            member _.SubscribeToJobState(state : JobState, action : Job Action) : IDisposable =
                raise <| new NotImplementedException()
            /// Job state is one of Stopped, Completed, or Failed.
            member this.IsFinished =
                this.Finished.WaitOne(0)
            /// Job state is one of Stopped, Completed, Failed, Disconnected, Suspended, or Blocked.
            member this.IsHalted =
                let jobState = this.JobStateInfo.State
                match jobState with
                | JobState.Completed
                | JobState.Stopped
                | JobState.Failed
                | JobState.Disconnected
                | JobState.Suspended
                | JobState.Blocked -> true
                //| JobState.NotStarted
                //| JobState.Running
                //| JobState.Stopping
                //| JobState.AtBreakpoint
                //| JobState.Suspending 
                | _ -> false
            /// Job is not started.
            member this.IsNotStarted =
                this.JobStateInfo.State = JobState.NotStarted
        
        

    open JobExtensions
    [<AbstractClass>]
    type JobBase =
        inherit Job

        override this.Location = GetDefaultJobLocation this
        override this.HasMoreData = GetDefaultHasMoreData this
        override this.StatusMessage = GetDefaultStatusMessage this

        new () as this = { inherit Job () } then this.PSJobTypeName <- this.GetType().Name
        new (command) as this = { inherit Job (command) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name) as this = { inherit Job (command, name) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name, childJobs : IList<Job>) as this = { inherit Job (command, name, childJobs) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name, instanceId : Guid ) as this = { inherit Job (command, name, instanceId) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name, token : JobIdentifier ) as this = { inherit Job (command, name, token) } then this.PSJobTypeName <- this.GetType().Name

    [<AbstractClass>]
    type Job2Base =
        inherit Job2
        override this.Location = GetDefaultJobLocation this
        override this.HasMoreData = GetDefaultHasMoreData this
        override this.StatusMessage = GetDefaultStatusMessage this

        override this.StartJobAsync () =
            this.StartJob()
            this.OnStartJobCompleted(new ComponentModel.AsyncCompletedEventArgs(null, false, null))
        
        override this.SuspendJobAsync () = this.SuspendJobAsync(false, null)
        override this.SuspendJobAsync (force, reason) =
            this.SuspendJob(force, reason)
            this.OnSuspendJobCompleted(new ComponentModel.AsyncCompletedEventArgs(null, false, null))
        override this.SuspendJob () = this.SuspendJob(false, null)
        override this.SuspendJob (force, reason) = new NotSupportedException() |> raise
        
        override this.StopJobAsync () = this.StopJobAsync(false, null)
        override this.StopJobAsync (force, reason) =
            this.StopJob(force, reason)
            this.OnStopJobCompleted(new ComponentModel.AsyncCompletedEventArgs(null, false, null))
        override this.StopJob () = this.StopJob(false,null)
        
        override this.ResumeJobAsync () =
            this.ResumeJob ()
            this.OnResumeJobCompleted(new ComponentModel.AsyncCompletedEventArgs(null, false, null))
        override this.ResumeJob () = new NotSupportedException () |> raise

        override this.UnblockJobAsync () =
            this.UnblockJob ()
            this.OnUnblockJobCompleted(new ComponentModel.AsyncCompletedEventArgs(null, false, null))
        override this.UnblockJob () = new NotSupportedException () |> raise

        
        new () as this = { inherit Job2 () } then this.PSJobTypeName <- this.GetType().Name
        new (command) as this = { inherit Job2 (command) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name) as this = { inherit Job2 (command, name) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name, childJobs : IList<Job>) as this = { inherit Job2 (command, name, childJobs) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name, instanceId : Guid ) as this = { inherit Job2 (command, name, instanceId) } then this.PSJobTypeName <- this.GetType().Name
        new (command, name, token : JobIdentifier ) as this = { inherit Job2 (command, name, token) } then this.PSJobTypeName <- this.GetType().Name