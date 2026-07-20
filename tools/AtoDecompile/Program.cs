using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

// AtoDecompile <assembly.dll> <output.cs> [--type=Name ...] [extraSearchDir ...]
//
//  * With no --type args: decompiles the whole module to one C# file.
//  * With one or more --type=Name args: decompiles only those types (great for
//    inspecting large assemblies like Assembly-CSharp without dumping everything).
//  Extra existing directories are added as assembly-resolver search paths.

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: AtoDecompile <assembly.dll> <output.cs> [--type=Name ...] [extraSearchDir ...]");
    return 1;
}

string asmPath = Path.GetFullPath(args[0]);
string outPath = Path.GetFullPath(args[1]);

var typeNames = new List<string>();
var extraDirs = new List<string>();
foreach (string a in args.Skip(2))
{
    if (a.StartsWith("--type=", StringComparison.OrdinalIgnoreCase))
        typeNames.Add(a.Substring("--type=".Length));
    else if (Directory.Exists(a))
        extraDirs.Add(a);
}

if (!File.Exists(asmPath))
{
    Console.Error.WriteLine($"Assembly not found: {asmPath}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

var settings = new DecompilerSettings
{
    ThrowOnAssemblyResolveErrors = false,
    ShowXmlDocumentation = false,
    UseNestedDirectoriesForNamespaces = false,
};

var resolver = new UniversalAssemblyResolver(asmPath, throwOnError: false, targetFramework: null);
resolver.AddSearchDirectory(Path.GetDirectoryName(asmPath)!);
foreach (var d in extraDirs)
{
    resolver.AddSearchDirectory(d);
    Console.WriteLine($"[resolver] search dir: {d}");
}

var decompiler = new CSharpDecompiler(asmPath, resolver, settings);

string code;
if (typeNames.Count > 0)
{
    var sb = new System.Text.StringBuilder();
    foreach (string name in typeNames)
    {
        Console.WriteLine($"[decompile type] {name}");
        try
        {
            sb.AppendLine($"// ===================== {name} =====================");
            sb.AppendLine(decompiler.DecompileTypeAsString(new FullTypeName(name)));
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"// !! Failed to decompile '{name}': {ex.Message}");
            Console.Error.WriteLine($"[warn] {name}: {ex.Message}");
        }
    }
    code = sb.ToString();
}
else
{
    Console.WriteLine($"[decompile module] {asmPath}");
    code = decompiler.DecompileWholeModuleAsString();
}

File.WriteAllText(outPath, code);
Console.WriteLine($"[done] wrote {code.Length:N0} chars -> {outPath}");
return 0;
