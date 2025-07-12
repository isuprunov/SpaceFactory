using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RpcGenerator;



[Generator]
public class RpcEndpointGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var playerClass = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax cds && cds.Identifier.Text == "Player",
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(static cls => cls is not null);

        context.RegisterSourceOutput(playerClass, static (ctx, classDecl) =>
        {
            var methods = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m =>
                    m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) &&
                    !m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)) && 
                    m.AttributeLists.SelectMany(a => a.Attributes).All(attr => attr.Name.ToString() != "IgnoreRpcMethod"));
            
            var sb = new StringBuilder();
            sb.AppendLine("public static class PlayerEndpoint");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic static void RegisterEndpoint(IEndpointRouteBuilder routBuilder, Dictionary<string, Game.Server.Player> players, Mutex mutex)");
            sb.AppendLine("\t{");
            foreach (var methodDeclarationSyntax in methods)
            {
                
                var parametersWithTypes = string.Join(", ", methodDeclarationSyntax.ParameterList.Parameters.Select(p => p.Type + " " + p.Identifier.Text));
                var parameters = string.Join(", ", methodDeclarationSyntax.ParameterList.Parameters.Select(p => p.Identifier.Text)); 
                
                sb.AppendLine($"\t\troutBuilder.MapGet(\"/api/{methodDeclarationSyntax.Identifier.Text}\" , (HttpContext ctx{(parametersWithTypes == string.Empty ? string.Empty : ", " + parametersWithTypes)}) =>");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tvar player = players[ctx.Request.Headers[\"playerId\"]!];");
                sb.AppendLine("\t\t\tmutex.WaitOne();");
                if(methodDeclarationSyntax.ReturnType.ToString() != "void")
                    sb.AppendLine($"\t\t\tvar res = player.{methodDeclarationSyntax.Identifier.Text}({parameters});");
                else 
                    sb.AppendLine($"\t\t\tplayer.{methodDeclarationSyntax.Identifier.Text}({parameters});");
                sb.AppendLine("\t\t\tmutex.ReleaseMutex();");
                if(methodDeclarationSyntax.ReturnType.ToString() != "void")
                    sb.AppendLine($"\t\t\treturn res;");
                sb.AppendLine("\t\t});");
                //sb.AppendLine("pub")
            }
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            
            ctx.AddSource("Rpc.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

            
        });
        
        // context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
        //     "Test.g.cs", 
        //     SourceText.From(playerClass.ToString(), Encoding.UTF8)));
    }
}