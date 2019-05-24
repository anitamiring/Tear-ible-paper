using Mono.Cecil;

namespace Apkd.Weaver
{
    interface IWeaver
    {
        void ProcessAssembly();
        void Initialize(AssemblyDefinition assembly);
        int Priority { get; }
    }

    interface IEarlyWeaver : IWeaver { }

    interface ILateWeaver : IWeaver { }
}