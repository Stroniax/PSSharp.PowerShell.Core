# PSSharp.Core.Independant


## Synopsis
C# project for PSSharp functionality that does not require external dependencies.

## Description

### TypeDataAnalysis
This project exposes attributes which may be used to define PowerShell TypeData through code analysis
which is supported by the PSSharp.Core PowerShell module.

### Tasks
This project utilizes tasks in a more lightweight syntax than is possible with F# by using the C# task-based asynchronous
programming model supported by the C# compiler. The project also takes advantage of the dynamic syntax supported in C#
that is missing in F# to provide additional support for any types that are considered "task-like".
