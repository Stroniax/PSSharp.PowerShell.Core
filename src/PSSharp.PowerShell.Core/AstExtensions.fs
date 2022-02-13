namespace PSSharp
open System
open System.Management.Automation
open System.Management.Automation.Language
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Collections.Generic

[<AutoOpen>]
[<Extension>]
[<AbstractClass; Sealed>]
type AstExtensions () =
    /// Gets an array representation of a sequence. If the sequence is already an array
    /// it will be returned as is.
    static let toArray (seq: IEnumerable<_>) =
        match seq with
        | :? ('T array) as arr -> arr
        | _ -> seq |> Seq.toArray
    static let castToArray source =
        source |> toArray |> Array.map unbox
    /// Returns true if the ast matches the type and the predicate passes.
    static let ofTypeAndPredicate (predicate: Func<'a, bool>) (ast: Ast) =
        match ast with
        | :? 'a as t -> predicate.Invoke(t)
        | _ -> false
    static let predicateOfScript (script: ScriptBlock) (arg: 'a) =
        let variables = new List<PSVariable>()
        new PSVariable(SpecialVariables.psItem, arg)
        |> variables.Add
        script.InvokeWithContext(null, variables, arg)
        |> LanguagePrimitives.IsTrue
        
    /// Fails if t is not assignable to System.Management.Automation.Language.Ast
    static member private AssertDerivedFromAst(t: Type, argName: string) =
        if not <| t.IsAssignableTo(typeof<Ast>) then
            raise <| new ArgumentException(ErrorMessages.TypeNotDerivedFromAst, argName)
    /// Finds all asts of a given type and returns an array with each value.
    [<Extension>]
    static member FindAll<'T when 'T :> Ast>(
            ast: Ast,
            [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) : 'T array  =
        ast.FindAll(new Func<Ast, bool>(fun i -> i :? 'T), searchNestedScriptBlocks)
        |> castToArray
    /// Finds all asts of a given type that match an additional predicate.
    [<Extension>]
    static member FindAll<'T when 'T :> Ast>(
            ast: Ast,
            predicate: Func<'T, bool>,
            [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) : 'T array =
        ast.FindAll(ofTypeAndPredicate(predicate), searchNestedScriptBlocks)
        |> castToArray
    /// Finds all asts of the type provided that match a given predicate.
    [<Extension>]
    static member FindAll(
            ast: Ast,
            ``type`` : Type,
            predicate: Func<Ast, bool>,
            [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) =
        AssertDerivedFromAst(``type``, nameof ``type``)
        ast.FindAll((fun child -> child.GetType().IsAssignableTo(``type``) && predicate.Invoke(child)), searchNestedScriptBlocks)
        |> toArray
    /// Finds all asts of the type provided.
    [<Extension>]
    static member FindAll(
            ast: Ast,
            ``type``: Type,
            [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) =
        AssertDerivedFromAst(``type``, nameof ``type``)
        ast.FindAll((fun child -> child.GetType().IsAssignableTo(``type``)), searchNestedScriptBlocks)
        |> toArray
    /// Finds the uppermost Ast parent of the current ast and returns it.
    [<Extension>]
    static member GetUppermostParent(ast: Ast) =
        if ast.Parent = null then ast
        else ast.Parent.GetUppermostParent()

    // --------------- --------------- ---------------
    // --------------- PS Code Methods ---------------
    // --------------- --------------- ---------------

    static member FindAll(
        ast: pso,
        ``type``: Type,
        [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) =
        let ast : Ast = ast |> PSCodeMethod.cast
        ast.FindAll(``type``, searchNestedScriptBlocks)
    static member FindAll(
        ast: pso,
        ``type``: Type,
        predicate: Func<Ast, bool>,
        [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) =
        let ast: Ast = ast |> PSCodeMethod.cast
        ast.FindAll(``type``, predicate, searchNestedScriptBlocks)
    static member FindAll(
        ast: pso,
        ``type``: Type,
        predicate: ScriptBlock,
        [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) =
        let ast: Ast = ast |> PSCodeMethod.cast
        ast.FindAll(``type``, predicateOfScript predicate, searchNestedScriptBlocks)
    static member FindAll(
        ast: pso,
        predicate: ScriptBlock,
        [<Optional;DefaultParameterValue(false)>] searchNestedScriptBlocks: bool
        ) =
        let ast: Ast = ast |> PSCodeMethod.cast
        ast.FindAll(predicateOfScript predicate, searchNestedScriptBlocks)