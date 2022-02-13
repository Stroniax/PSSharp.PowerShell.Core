namespace PSSharp.Commands
open PSSharp
open System
open System.Management.Automation

[<AbstractClass>]
type ExtendedTypeDataAnalysisCmdlet internal () =
    inherit PSCmdlet ()
    static let assemblyLoadEventHandler =
        new AssemblyLoadEventHandler(fun sender e ->
           
           ()
        )

    static member internal AssemblyLoadEventHandler = assemblyLoadEventHandler


/// Register an event subscriber to import type data from assemblies as they are loaded into the application domain.
type RegisterTypeExtensionCodeAnalysisCommand () =
    inherit PSCmdlet ()

