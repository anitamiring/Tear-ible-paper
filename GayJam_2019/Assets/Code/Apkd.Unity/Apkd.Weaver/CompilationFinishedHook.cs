using System;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using Mono.Cecil;
using Mono.Cecil.Mdb;
using static System.Reflection.BindingFlags;
using System.Collections.Generic;

namespace Apkd.Weaver
{
    [UnityEditor.InitializeOnLoad]
    public static class CompilationFinishedHook
    {
        static CompilationFinishedHook()
        {
            CompilationPipeline.assemblyCompilationStarted += (assemblyPath) =>
                InjectDelegateBeforeAndAfter(OnAssemblyCompilationFinishedEarly, OnAssemblyCompilationFinishedLate);
        }

        static readonly Dictionary<Type, long> stopwatchDurations = new Dictionary<Type, long>();

        static void InjectDelegateBeforeAndAfter(Action<string, CompilerMessage[]> before, Action<string, CompilerMessage[]> after)
        {
            CompilationPipeline.assemblyCompilationFinished -= before;
            CompilationPipeline.assemblyCompilationFinished -= after;

            var field = typeof(CompilationPipeline)
                .GetField(nameof(CompilationPipeline.assemblyCompilationFinished), NonPublic | Static);

            var originalDelegate = field.GetValue(null) as MulticastDelegate;

            field.SetValue(null, Delegate.Combine(before, originalDelegate, after));
        }

        static readonly string[] weavedAssemblyNames = { "Assembly-CSharp.dll", "Apkd.Pooling.dll" };

        static void OnAssemblyCompilationFinishedEarly(string assemblyPath, CompilerMessage[] messages)
        {
            if (!weavedAssemblyNames.Contains(Path.GetFileName(assemblyPath)))
                return;

            PostProcessAssembly<IEarlyWeaver>(assemblyPath);
        }

        static void OnAssemblyCompilationFinishedLate(string assemblyPath, CompilerMessage[] messages)
        {
            if (!weavedAssemblyNames.Contains(Path.GetFileName(assemblyPath)))
                return;

            PostProcessAssembly<ILateWeaver>(assemblyPath);
            UnityEngine.Debug.Log($"Assembly {assemblyPath} post-processing completed ({stopwatchDurations.Values.Sum()}ms). {stopwatchDurations.Aggregate("\n", (l, r) => $"{l}\n{r.Key.Name} - {r.Value}ms")}\n");
        }

        static void PostProcessAssembly<TWeaver>(string assemblyPath) where TWeaver : IWeaver
        {
            string outputDirectory = Path.GetDirectoryName(assemblyPath);

            {
                Mono.Cecil.Cil.ISymbolReaderProvider GetSymbolReaderProvider()
                    => typeof(TWeaver) == typeof(IEarlyWeaver)
                        ? new Mono.Cecil.Pdb.PdbReaderProvider() as Mono.Cecil.Cil.ISymbolReaderProvider
                        : new Mono.Cecil.Mdb.MdbReaderProvider();

                var reader = new ReaderParameters()
                {
                    ReadSymbols = true,
                    SymbolReaderProvider = GetSymbolReaderProvider(),
                };
                var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, reader);

                var resolver = assembly.MainModule.AssemblyResolver as DefaultAssemblyResolver;
                resolver.AddSearchDirectory(Path.GetDirectoryName(UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath()));
                resolver.AddSearchDirectory(Path.GetDirectoryName(UnityEditorInternal.InternalEditorUtility.GetEngineAssemblyPath()));
                resolver.AddSearchDirectory(Path.Combine(System.Environment.CurrentDirectory, "Library", "ScriptAssemblies"));
                // Debug.Log(resolver.GetSearchDirectories().Aggregate("searchdirs:\n\n", (l, r) => $"{l}\n{r}"));

                var weavers = typeof(TWeaver)
                    .Assembly
                    .GetTypes()
                    .Where(x => !x.IsAbstract && !x.IsInterface && typeof(TWeaver).IsAssignableFrom(x))
                    .Select(x => Activator.CreateInstance(x))
                    .Cast<TWeaver>()
                    .OrderBy(x => x.Priority)
                    .ToArray();

                if (weavers.Length > 0)
                {
                    foreach (var weaver in weavers)
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        weaver.Initialize(assembly);
                        weaver.ProcessAssembly();
                        stopwatchDurations[weaver.GetType()] = sw.ElapsedMilliseconds;
                    }

                    var writer = new WriterParameters()
                    {
                        WriteSymbols = true,
                        SymbolWriterProvider = new MdbWriterProvider(),
                    };
                    assembly.Write(assemblyPath, writer);
                }
            }
        }
    }
}