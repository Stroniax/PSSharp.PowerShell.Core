namespace PSSharp
open System
open System.Management.Automation

module ErrorMessages =
    
    [<CompiledName("ParameterSetNotImplemented")>]
    let parameterSetNotImplemented (parameterSetName : string) =
        let ex = new NotImplementedException(ErrorMessages.ParameterSetNotImplemented)
        let er = new ErrorRecord(
            ex,
            nameof ErrorMessages.ParameterSetNotImplemented,
            ErrorCategory.NotImplemented,
            parameterSetName)
        er.ErrorDetails <- new ErrorDetails(
            String.Format(ErrorMessages.ParameterSetNotImplementedInterpolated, parameterSetName)
            )
        er.ErrorDetails.RecommendedAction <- ErrorMessages. NotImplementedHelpMessage
        er
        
    let psCodeMethodInvalidSourceInterpolated expected =
        new ArgumentException(
            String.Format(ErrorMessages.PSCodeMethodInvalidSourceInterpolated, [|expected|]),
            "this"
            )