namespace PSSharp.Commands
open System
open System.Management.Automation
open System.Net.NetworkInformation
open PSSharp

[<Cmdlet(VerbsLifecycle.Start, Nouns.PingJob)>]
type StartPingJobCommand () =
    inherit PSCmdlet ()

    /// Endpoint computer host name or IP address to ping.
    [<Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)>]
    [<Alias("IPAddress", "HostName", "MachineName", "PSComputerName", "cn")>]
    [<AstStringConstantCompletion>]
    member val ComputerName = String.Empty with get, set

    /// Milliseconds until timeout of each ping request sent.
    /// This value must be positive (Timeout.Infinite is not supported).
    /// The default value is 5000.
    [<Parameter(Position = 1)>]
    [<Alias("to")>]
    [<ConstantCompletion("5000", ToolTip = "Default ping timeout value of 5000 ms")>]
    [<ValidateRange(1, Int32.MaxValue)>]
    member val Timeout = 5000 with get, set

    /// Buffer to send with the Ping. Leave null to use the default buffer.
    [<Parameter(Position = 2)>]
    [<Alias("bf")>]
    [<NoCompletion>]
    member val Buffer : byte array = null with get, set

    /// Options to configure how the Ping request is sent.
    [<Parameter(Position = 3)>]
    [<NoCompletion>]
    member val Options: PingOptions = null with get, set

    /// Maximum number of ping attempts to send before the job is aborted.
    [<Parameter(Position = 4)>]
    [<NumericCompletion(1.0, 1.0, Max = 2147483647.0)>]
    [<ValidateRange(1, Int32.MaxValue)>]
    [<Alias("ma", "Attempts")>]
    member val MaximumAttempts = Int32.MaxValue with get, set

    override this.ProcessRecord () =
        let job = new PingJob(this.ComputerName, this.Timeout, this.MaximumAttempts, this.Buffer, this.Options)
        this.JobRepository.Add(job)
        job.StartJob ()
        job |> this.WriteObject
        