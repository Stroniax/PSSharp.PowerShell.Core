@{
    # Use this file to customize actions for the build.ps1 script that runs when you build the module

    # Relative path from `$PSScriptRoot to where the output should be built
    # The module name and version will be appended to this directory, i.e. '.' will build at '$PSScriptRoot\MyModule\1.0'
    'OutputFolder' = '.'
    # The name of the module to build, which will be used in the build output path and when naming generated
    # components such as the script module file.
    'ModuleName' = 'PSSharp.PowerShell.Core'
    # The version to build. Will be used in the manifest and in the build output path.
    'Version' = '1.0.0'
    # True to set the AssemblyVersion and AssemblyFileVersion of binary projects to the version specified above
    # before building the file(s).
    'SetAssemblyVersionOnBuild' = $true
    'CompileScriptModules' = $true

    'IncludeFiles' = @(
        # One or more paths relative to the directory of this file indicating files that should be copied into the
        # module output. By default, only types.ps1xml and format.ps1xml files will be copied directly. The binary
        # project will be published into the output folder, script .ps1 and .psm1 files within the src/PowerShell
        # directory will be compiled into the output folder as 'PSSharp.PowerShell.Core.psm1', .cdxml files within
        # src/PowerShell/cdxml will be copied directly.
        # Files in this list will be placed at './Resources/${FileName}' in the module during build.
    )
    # Paths to binary projects to build using dotnet build. Hashtable of project name to project path.
    'BinaryProjectPaths' = @{
        'PSSharp.PowerShell.Core' = 'src/PSSharp.PowerShell.Core'
    }
    # Script modules to build. Hashtable of script module name to directories containing the script files.
    'ScriptModuleProjectPaths' = @{
        'PSSharp.PowerShell.Core' = 'src/PowerShell'
    }
    # Paths to exclude from script module build
    'ScriptFilesToExclude' = @()

    # Paths from which PlatyPS docs should be generated
    'DocumentationPaths' = @(
        'src/Documentation'
    )

    'Manifest' = @{
        # 'CompanyName' = ''
        # 'Author' = ''
        # 'LicenseUri' = 'https://opensource.org/licenses/MIT'
        # 'Copyright' = ''
        # 'ProjectUri' = ''
        # 'Description' = ''
        # 'ReleaseNotes' = ''
        # 'Prerelease' = ''
        # 'ScriptsToProcess' = @()
        # 'ProcessorArchitecture' = ''
        # 'ClrVersion' = ''
        # 'DotNetFrameworkVersion' = ''
        # 'PowerShellHostName' = ''
        # 'PowerShellHostVersion' = ''
        'RequiredModules' = @('PSSharp.PowerShell.FSharpCore')
        # 'ModuleList' = @()
        # 'DscResourcesToExport' = @()
        # 'PrivateData' = @{}
        # 'Tags' = @()
        # 'IconUri' = ''
        # 'RequireLicenseAcceptance' = $false
        # 'ExternalModuleDependencies' = @()
        # 'HelpInfoUri' = ''
        # 'DefaultCommandPrefix' = ''
        # 'VariablesToExport' = @()

        # The members below may be manually overridden but values will be auto-generated by default.

        # Generated using New-Guid.
        # 'Guid' = ''

        # Assemblies will be imported into a private session and enumerated to populate this field.
        # 'CmdletsToExport' = @()

        # Files in the src/PowerShell/Functions/Public folder will be used to identify public functions.
        # 'FunctionsToExport' = @()

        # Aliases using the [Alias()] attribute will be used to populate this field.
        # 'AliasesToExport' = @()

        # This field will be populated from all *.types.ps1xml files in the output at the end of the build.
        # 'TypesToProcess' = @()

        # This field will be populated from all *.format.ps1xml files in the output at the end of the build.
        # 'FormatsToProcess' = @()

        # This field will not be included; the module type will be 'Manifest' and modules (such as the
        # script module and binary modules) will be listed under 'NestedModules'.
        # RootModule = "PSSharp.Core$($NoBinaryProject ? '.psm1' : '.dll')""

        # Generated from '#Requires -Version' statements in script files
        # 'PowerShellVersion' = '7.2.0'

        # Generated from all dll files in the output folder at the end of the build, and '#Requires -Assembly'
        # statements in script files.
        # Manual override will list the files required instead of including all in the output folder, but
        # assemblies indicated by #Requires -Assembly in script modules files will still be appended to this
        # list.
        'RequiredAssemblies' = @('PSSharp.PowerShell.Core.dll', 'PSSharp.PowerShell.Core.Tasks.dll', 'PSSharp.PowerShell.Core.Independant.dll')

        # Generated from '#Requires -PSEdition' statements in script files. If overridden, cannot conflict
        # with a PSEdition in a script file.
        # 'CompatiblePSEditions' = @()

        # The following will be generated during the build process. Overriding the value here will be ignored.
        # ModuleVersion         - from the Version parameter of the root hashtable defined in this file
        # FileList              - from all contents of the output directory
    }
}
