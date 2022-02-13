namespace PSSharp
open System
open System.Management.Automation
open System.Management.Automation.Language
open ArgumentCompletion
//open AstExtensions

/// Offers completions from highest reachable AST from the caller for any
/// StringConstantExpressionAst asts.
type AstStringConstantCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    static let rec getTopLevelParentAst (ast : Ast) =
        if ast.Parent = null then ast
        else getTopLevelParentAst ast.Parent

    override _.CompleteArgument(_, _, wordToComplete, commandAst, _) =
        let { QuotationType = quoteType; UnquotedText = strippedWordToComplete } = trimQuotes wordToComplete
        let wc = WildcardPattern.Get (strippedWordToComplete + "*", WildcardOptions.IgnoreCase)
        let topLevelAst = getTopLevelParentAst commandAst
        [
            for ast in topLevelAst.FindAll<StringConstantExpressionAst>(true) do
                if wc.IsMatch ast.Value then
                    let completionText = ast.ToString()
                    new CompletionResult(
                        completionText,
                        ast.Value,
                        CompletionResultType.ParameterValue,
                        ast.Value
                        )
        ]
