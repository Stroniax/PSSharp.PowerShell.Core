namespace PSSharp
open System
open System.Management.Automation

type NumericCompletionAttribute (start : double, step: double) =
    inherit CompletionBaseAttribute ()
    
    static let getCompletion value =
        new CompletionResult(
            string value,
            string value,
            CompletionResultType.ParameterValue,
            string value)
    
    new() = NumericCompletionAttribute(0.0, 1.0)

    /// Number of completion results to load at a time.
    member val Quantity = 100 with get, set
    member val Max = Double.MaxValue with get, set

    override this.CompleteArgument(_, _, wordToComplete, _, _) =   
        if String.IsNullOrEmpty wordToComplete then
            let mutable s = start
            let mutable r = 0
            [
                while s < this.Max && r < this.Quantity do
                    let c = getCompletion s
                    s <- s + step
                    r <- r + 1
                    c
            ]
        else []