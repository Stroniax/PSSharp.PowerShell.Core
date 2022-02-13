namespace PSSharp
open System
open System.Management.Automation
open System.Management.Automation.Runspaces
open System.Management.Automation.Remoting

[<Struct>]
type PSInformationalStream =
/// The stream is not defined.
| Undefined
/// The output or success stream. This stream has redirection id 1.
| Output
/// The error stream. This stream has redirection id 2.
| Error
/// The warning stream. This stream has redirection id 3.
| Warning
/// The verbose stream. This stream has redirection id 4.
| Verbose
/// The debug stream. This stream has redirection id 5.
| Debug
/// The information stream. This stream has redirection id 6.
| Information
/// The progress stream. This stream may not be redirected.
| Progress

/// An wrapper that may contain any item from PSInformationalBuffers.
type PSInformationalItem =
/// Value unknown
| Undefined
/// Debug information.
| Debug of debug: DebugRecord
/// Informational details.
| Information of information: InformationRecord
/// Verbose explanation.
| Verbose of verbose: VerboseRecord
/// Warning of a possibly unexpected state or result.
| Warning of warning: WarningRecord
/// Reports an error state.
| Error of error: ErrorRecord
/// An output object.
| Output of object: pso
/// Update to the progress of an operation.
| Progress of progress: ProgressRecord
    /// Gets the stream this item is associated with.
    member this.GetInformationalStream() =
        match this with
        | Undefined-> PSInformationalStream.Undefined
        | Output _ -> PSInformationalStream.Output
        | Error _ -> PSInformationalStream.Error
        | Warning _ -> PSInformationalStream.Warning
        | Verbose _ -> PSInformationalStream.Verbose
        | Debug _ -> PSInformationalStream.Debug
        | Information _ -> PSInformationalStream.Information
        | Progress _ -> PSInformationalStream.Progress

/// Delegate representing an operation that writes an item to one of the PowerShell streams.
type PSInformationalItemWriter = delegate of obj: PSInformationalItem -> unit

module PSInformationalItemWriter =
    [<CompiledName("OfCmdlet")>]
    let ofCmdlet (cmdlet: Cmdlet) =
        new PSInformationalItemWriter(fun item ->
            match item with
            | Undefined -> ()
            | Output o -> cmdlet.WriteObject o
            | Error e -> cmdlet.WriteError e
            | Warning w -> cmdlet.WriteWarning w.Message
            | Verbose v -> cmdlet.WriteVerbose v.Message
            | Debug d -> cmdlet.WriteDebug d.Message
            | Information i -> cmdlet.WriteInformation i
            | Progress p -> cmdlet.WriteProgress p
            )
    [<CompiledName("OfJob")>]
    let ofJob (job: Job, onTerminatingError) =
        new PSInformationalItemWriter(fun item ->
            match item with
            | Undefined -> ()
            | Output o -> job.Output.Add o
            | Error e -> job.Error.Add e
            | Warning w -> job.Warning.Add w
            | Verbose v -> job.Verbose.Add v
            | Debug d -> job.Debug.Add d
            | Information i -> job.Information.Add i
            | Progress p -> job.Progress.Add p
            )
