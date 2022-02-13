namespace PSSharp
open System
open System.Collections.Generic
open System.Management.Automation

type ScriptTransformationAttribute (script : ScriptBlock) =
    inherit ArgumentTransformationAttribute ()
    
    /// Create a script from the specified string to use for transformation.
    /// This is particularly useful from compiled code where a ScriptBlock is not
    /// a valid attribute argument type.
    new (scriptText : string) =
        new ScriptTransformationAttribute(ScriptBlock.Create(scriptText))

    override _.Transform (engineIntrinsics, inputData) =
        let variables = new List<PSVariable> ([|
            new PSVariable(SpecialVariables.psItem, inputData)
            new PSVariable(SpecialVariables.input, inputData)
            new PSVariable(nameof engineIntrinsics, engineIntrinsics)
            new PSVariable(nameof inputData, inputData)
        |])

        let values = script.InvokeWithContext(null, variables, inputData, engineIntrinsics)
        flattenOrMutate values box
        