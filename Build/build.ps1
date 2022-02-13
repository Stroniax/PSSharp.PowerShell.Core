#requires -Module PlatyPS
#requires -Module PSSharp.ModuleFactory

[CmdletBinding()]
param()

process {
    Push-Location -StackName 'Build.ps1'
    try {
        Set-Location (Split-Path $PSScriptRoot -Parent)
        $Data = Import-PowerShellDataFile -Path (Join-Path $PSScriptRoot 'Build.psd1')
        $Manifest = $Data['Manifest']
        [version]$Version = ($Data['Version'] -as [version]) ?? '1.0.0'
        $ModuleName = $Data['ModuleName']
        $OutputRoot = Join-Path $PSScriptRoot $Data['OutputFolder']
        $OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath((Join-Path $OutputRoot $ModuleName $Version))
        if (!(Test-Path $OutputPath)) {
            New-Item -ItemType Directory -Path $OutputPath
        }
        [System.Collections.Generic.List[string]]$NestedModules = @()
        $AddRangeParameters = @{
            MemberType = 'ScriptMethod'
            Name = 'AddRange'
            Value = { param([string[]]$values) foreach ($val in $values) { [void]$this.Add($val) } }
            PassThru = $true
        }
        function newhashset { ,[System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase) }
        $RequiredAssemblies = Add-Member -InputObject (newhashset) @AddRangeParameters
        $FormatsToProcess = Add-Member -InputObject (newhashset) @AddRangeParameters
        $TypesToProcess = Add-Member -InputObject (newhashset) @AddRangeParameters
        $FunctionsToExport = Add-Member -InputObject (newhashset) @AddRangeParameters
        $AliasesToExport = Add-Member -InputObject (newhashset) @AddRangeParameters
        $CmdletsToExport = Add-Member -InputObject (newhashset) @AddRangeParameters
        $RequiredModules = [System.Collections.Generic.List[object]]::new()

        [version]$PowerShellVersion = $null

        # Build binary modules
        $GetExportedCommandsScript = {
            Set-Location $using:OutputPath
            Import-Module 'PSSharp.PowerShell.FSharpCore'
            $m = Import-Module "./$using:binaryProject.dll" -PassThru
            if (!$m) { Write-Error "Failed to import binary module '$using:binaryproject.dll' to remote sesion for command identification." }
            @($m.ExportedCmdlets.Values; $m.ExportedAliases.Values) | Select-Object -Property Name, @{L='CommandType';E={$_.CommandType.ToString()}}
        }
        foreach ($binaryProject in $Data['BinaryProjectPaths'].Keys) {
            $binaryProjectPath = $Data['BinaryProjectPaths'][$binaryProject]
            Write-Verbose "Building binary module '$BinaryProject' from path '$binaryProjectPath'."
            [void]$NestedModules.Add("$binaryProject.dll")
            [void]$RequiredAssemblies.Add("$binaryProject.dll")
            if ($Data['SetAssemblyVersionOnBuild']) {
                dotnet publish $binaryProjectPath --output $OutputPath /p:Version="$Version" /p:AssemblyFileVersion="$Version" /p:AssemblyVersion="$Version"
            }
            else {
                dotnet publish $binaryProjectPath --output $OutputPath
            }

            $cmdletAlias = Start-Job -ScriptBlock $GetExportedCommandsScript | Receive-Job -Wait -AutoRemoveJob
            $CmdletsToExport.AddRange($cmdletAlias.Where{$_.CommandType -eq 'Cmdlet'}.Name)
            $AliasesToExport.AddRange($cmdletAlias.Where{$_.CommandType -eq 'Alias'}.Name)
        }

        # Build script modules
        $ExcludeFiles = if ($Data['ScriptFilesToExclude']) { Get-Item -Path $Data['ScriptFilesToExclude'] -ErrorAction Ignore } else { @() }
        foreach ($scriptProject in $Data['ScriptModuleProjectPaths'].Keys) {
            $scriptProjectPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Data['ScriptModuleProjectPaths'][$scriptProject])
            if (!(Test-Path $ScriptProjectPath)) {
                Write-Warning "No files were identified for script module project $scriptProject at path '$scriptProjectPath'."
                continue;
            }
            $ScriptProjectFiles = Get-ChildItem -Path $scriptProjectPath -Include '*.ps1', '*.psm1' -Recurse
            | Where-Object { $_.FullName -notin $ExcludeFiles.FullName }

            if ($Data['CompileScriptModules']) {
                Write-Verbose "Building script module '$scriptProject' from path '$scriptProjectPath'."
                $ScriptModuleData = $ScriptProjectFiles
                | Build-ScriptModule -DestinationPath (Join-Path $OutputPath "$scriptProject.psm1")

                if ($ScriptModuleData) {
                    # Only add the file if it was created
                    [void]$NestedModules.Add("$scriptProject.psm1")
                }

                $RequiredAssemblies.AddRange($ScriptModuleData.RequiredAssemblies)
                if ($ScriptModuleData.RequiredModules) {
                    $RequiredModules.AddRange($ScriptModuleData.RequiredModules)
                }
                if ($RequiredPSEditions -and ($RequiredPSEditions -ne $ScriptModuleData.RequiredPSEditions)) {
                    Write-Error 'Conflicting PSEdition requirements.'
                }
                elseif (!$RequiredPSEditions) {
                    $RequiredPSEditions = $ScriptModuleData.RequiredPSEditions
                }
                if ($ScriptModuleData.PowerShellVersion -and (($PowerShellVersion ?? '1.0') -lt $ScriptModuleData.PowerShellVersion)) {
                    $PowerShellVersion = $ScriptModuleData.PowerShellVersion
                }
                if ($ScriptModuleData.IsElevationRequired) {
                    $IsElevationRequired = $true
                }
            }
            else {
                Write-Verbose "Copying script module components for '$scriptProject' from path '$scriptProjectPath'."
                Write-Warning "When script files are not compiled, all aliases are exported and requirements from script modules are not included in the manifest."
                [void]$AliasesToExport.Add('*')
                $scriptProjectOutputRoot = Join-Path $OutputPath $scriptProject
                if (!(Test-Path $ScriptProjectOutputRoot)) {
                    New-Item -ItemType Directory -Path $scriptProjectOutputRoot
                }
                $copyTargets = $ScriptProjectFiles
                | ForEach-Object {
                    $copyTarget = [PSCustomObject]@{
                        From = $_.FullName
                        To = $_.FullName.Replace($scriptProjectPath, $scriptProjectOutputRoot, [StringComparison]::OrdinalIgnoreCase)
                        ShouldCopy = $true
                    }
                    $copyTargetDir = Split-Path $copyTarget.To -Parent
                    if (!(Test-Path $copyTargetDir)) {
                        New-Item -ItemType Directory -Path $copyTargetDir 1>$null
                    }
                    if ((Test-Path $copyTarget.To) -and ([System.IO.File]::GetLastWriteTime($copyTarget.To) -ge $_.LastWriteTime)) {
                        $copyTarget.ShouldCopy = $false
                    }
                    $copyTarget
                }
                $copyTargets | Where-Object ShouldCopy | Copy-Item -Path {$_.From} -Destination {$_.To}
                [string[]]$scriptProjectOutput = $copyTargets.To
                | ForEach-Object {
                    $_.Replace($outputPath, '', [StringComparison]::OrdinalIgnoreCase).Trim('/\')
                }
                if ($scriptProjectOutput) {
                    $nestedModules.AddRange($scriptProjectOutput)
                }
            }

            # Non-compiled script files
            $TypeFiles = Get-ChildItem $scriptProjectPath -Include '*.types.ps1xml' -Recurse
            | Where-Object { $_.FullName -notin $ExcludeFiles.FullName }
            $FormatFiles = Get-ChildItem $scriptProjectPath -Include '*.format.ps1xml' -Recurse
            | Where-Object { $_.FullName -notin $ExcludeFiles.FullName }
            $OutputTypeFiles = $TypeFiles | Copy-Item -Destination {Join-Path $OutputPath $_.Name} -PassThru
            $OutputFormatFiles = $FormatFiles | Copy-Item -Destination {Join-Path $OutputPath $_.Name} -PassThru
            $TypesToProcess.AddRange($OutputTypeFiles.Name)
            $FormatsToProcess.AddRange($OutputFormatFiles.Name)

            # Identify functions to export from the module
            $PublicFunctionsDir = Join-Path $scriptProjectPath 'Public'
            if (Test-Path $PublicFunctionsDir) {
                $FunctionNames = @(Get-ChildItem $PublicFunctionsDir -Recurse -File
                | Where-Object Name -like '*-*.ps*'
                | Select-Object -ExpandProperty BaseName)
                $FunctionsToExport.AddRange($FunctionNames)
                if ($ScriptModuleData) {
                    foreach ($FunctionName in $FunctionNames) {
                        if ($ScriptModuleData.Aliases.ContainsKey($FunctionName)) {
                            $AliasesToExport.AddRange($ScriptModuleData.Aliases[$FunctionName])
                        }
                    }
                }
            }
        }

        # Build documentation
        foreach ($helpFileSource in $Data['DocumentationPaths']) {
            Write-Verbose "Building help files from source '$helpFileSource'."
            New-ExternalHelp -OutputPath $OutputPath -Path $helpFileSource -Force
        }

        # Copy explicit copy files from the manifest
        foreach ($copyFile in $Data['IncludeFiles']) {
            if ($copyFile -is [string]) {
                $To = Join-Path $OutputPath (Split-Path $copyFile -Leaf)

                Copy-Item -Path $copyFile -Destination $To
            }
            elseif ($copyFile -is [hashtable]) {
                $From = $copyFile['From'] ?? $copyFile['Path'] ?? $copyFile['FilePath'] ?? $copyFile['Source']
                $To = $copyFile['To'] ?? $copyFile['Output'] ?? $copyFile['Destination']
                if (!$From -or !$To) {
                    Write-Error "The hashtable input $copyFile is invalid. The value must contain a From and To key to indicate the where the file exists relative to the project directory and where it should be placed relative to the output path."
                }
                else{
                    Copy-Item -Path $From -Destination $To
                }
            }
            else {
                Write-Error "Cannot process IncludeFile '$copyFile' of type $(${copyFile}?.GetType() ?? "(null)"). Data type not handled."
            }
        }

        function TrimPath {
            param(
                [Parameter(ValueFromPipelineByPropertyName)]
                [Alias('FullName')]
                [string[]]$FilePath
            )
            process {
                foreach ($fp in $FilePath) {
                    $fp.Replace($OutputPath, '', [StringComparison]::OrdinalIgnoreCase).Trim('/\')
                }
            }
        }
        $MaybeSetValueParameters = @{
            InputObject = $Manifest
            MemberType  = 'ScriptMethod'
            Name        = 'MaybeSetValue'
            Value       = {
                param([string]$key, [object]$value)

                if (!$this.ContainsKey($key)) {
                    $this[$key] = $value
                }
                # return $this[$key]
            }
        }
        Add-Member @MaybeSetValueParameters
        # Build manifest
        $Manifest['Path'] = Join-Path $OutputPath "$ModuleName.psd1"
        $Manifest['FileList'] = Get-ChildItem -Path $OutputPath -Recurse | TrimPath
        $Manifest['ModuleVersion'] = $Version
        $Manifest.MaybeSetValue('TypesToProcess', $TypesToProcess)
        $Manifest.MaybeSetValue('FormatsToProcess', $FormatsToProcess)
        $Manifest.MaybeSetValue('FunctionsToExport', $FunctionsToExport)
        $Manifest.MaybeSetValue('CmdletsToExport', @($CmdletsToExport))
        $Manifest.MaybeSetValue('AliasesToExport', @($AliasesToExport))
        if ($PowerShellVersion) {
            $Manifest['PowerShellVersion'] = ($Manifest['PowerShellVersion'] ?? '1.0.0') -gt $PowerShellVersion ? $Manifest['PowerShellVersion'] : $PowerShellVersion
        }
        if ($CompatiblePSEditions) {
            if ($Manifest.ContainsKey('CompatiblePSEditions')) {
                if (![System.Linq.Enumerable]::SequenceEqual([string[]]@($Manifest['CompatiblePSEditions']), [string[]]@($CompatiblePSEditions)), [StringComparer]::OrdinalIgnoreCase) {
                    Write-Error "The CompatiblePSEditions specified in the script module files does not match the CompatiblePSEditions in the build.psd1 manifest section."
                }
            }
            else {
                $Manifest['CompatiblePSEditions'] = $CompatiblePSEditions
            }
        }
        $Manifest['NestedModules'] = $NestedModules | Where-Object { $_ -ne $Manifest['RootModule'] }
        if (!$Manifest.ContainsKey('RequiredAssemblies')) {
            $AssembliesFromOutput = Get-ChildItem -Path $OutputPath -Recurse -Include '*.dll' | TrimPath
            $RequiredAssemblies.AddRange($AssembliesFromOutput)
            $Manifest['RequiredAssemblies'] = $RequiredAssemblies
        }
        else {
            $RequiredAssemblies.AddRange($Manifest['RequiredAssemblies'])
            $Manifest['RequiredAssemblies'] = [string[]]$RequiredAssemblies
        }
        New-ModuleManifest @Manifest
    }
    finally {
        Pop-Location -StackName 'Build.ps1'
    }
}
