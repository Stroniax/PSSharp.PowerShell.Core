using module ../../Build/PSSharp.Core
[CmdletBinding()]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '', Justification = 'Pester has special variable scope behavior')]
param()

Describe 'PSSharp.Core' {
    BeforeAll {
    }
    Context 'Command <Command>' -ForEach @(
        Get-Command -Module 'PSSharp.Core'
        | ForEach-Object {
            @{ Command = $_ }
        }
    ) {
        # Check that the command has a defined RemotingCapability
        It 'defines RemotingCapability' {
            if ($Command.CommandType -eq 'Function') {
                [System.Management.Automation.Language.FunctionDefinitionAst]$Ast = $Command.ScriptBlock.Ast
                [System.Management.Automation.Language.AttributeAst]$Attribute = $Ast.Body.ParamBlock.Attributes.Where{$_.TypeName.GetReflectionType() -eq [CmdletBinding]} | Select-Object -First 1
                $RemotingCapability = @($Attribute.NamedArguments.Where{$_.ArgumentName -eq 'RemotingCapability'})
                $RemotingCapability | Should -HaveCount 1
            }
            elseif ($Command.CommandType -eq 'Cmdlet') {
                [System.Reflection.CustomAttributeData]$Attribute = $Command.ImplementingType.GetCustomAttributesData().Where{$_.AttributeType -eq [System.Management.Automation.CmdletAttribute]} | Select-Object -First 1
                $RemotingCapability = @($Attribute.NamedArguments.Where{$_.MemberName -eq 'RemotingCapability'})
                $RemotingCapability | Should -HaveCount 1
            }
        }

        # Check that the command has a defined OutputType
        It 'defines OutputType' {
            if ($Command.CommandType -eq 'Function') {
                [System.Management.Automation.Language.FunctionDefinitionAst]$Ast = $Command.ScriptBlock.Ast
                [System.Management.Automation.Language.AttributeAst]$Attribute = $Ast.Body.ParamBlock.Attributes.Where{$_.TypeName.GetReflectionType() -eq [OutputType]} | Select-Object -First 1
                $RemotingCapability | Should -HaveCount 1
            }
            elseif ($Command.CommandType -eq 'Cmdlet') {
                [System.Reflection.CustomAttributeData]$Attribute = $Command.ImplementingType.GetCustomAttributesData().Where{$_.AttributeType -eq [OutputType]} | Select-Object -First 1
                $RemotingCapability | Should -HaveCount 1
            }
        }

        It 'has a default parameter set' {
            if ($Command.ParameterSets.Count -gt 1) {
                $Command.DefaultParameterSet | Should -Not -BeNullOrEmpty -Because 'commands must define a default parameter set'
                $Command.ParameterSets.Name | Should -Contain $Command.DefaultParameterSet -Because 'the default parameter set must be an actual parameter set'
            }
        }
        # Check that a default parameter set is defined if a parameter set exists
        It 'has completions for <Parameter.Name>' -ForEach @(
            $ignore = @([System.Management.Automation.Cmdlet]::CommonParameters + [System.Management.Automation.Cmdlet]::OptionalCommonParameters)
            $Command.Parameters.Values
            | Where-Object {
                $_.Name -notin $ignore -and
                $_.Name -notlike '*path' -and
                $_.ParameterType -notlike [switch] -and
                !$_.ParameterType.IsAssignableTo([Enum])
            }
            | ForEach-Object {
                @{ Parameter = $_ }
            }
        ) {
            $Completer = $Parameter.Attributes
            | Where-Object {
                $_ -is [ValidateSet] -or
                $_ -is [ArgumentCompletions] -or
                $_ -is [ArgumentCompleter] -or
                $_ -is [System.Management.Automation.ArgumentCompleterFactoryAttribute] -or
                ($_.GetType().Name -like '*PathTo*' -and $_ -is [System.Management.Automation.ArgumentTransformationAttribute])
            }
            $Completer | Should -Not -BeNullOrEmpty -Because 'parameters should offer relevant argument completion'
        }
    }
}
