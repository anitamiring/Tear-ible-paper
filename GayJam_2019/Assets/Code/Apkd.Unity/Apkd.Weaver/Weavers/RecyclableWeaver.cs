using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static System.Reflection.BindingFlags;
using static Mono.Cecil.Cil.Instruction;
using static Mono.Cecil.Cil.OpCodes;

namespace Apkd.Weaver
{
    sealed class RecyclableWeaver : ILateWeaver
    {
        AssemblyDefinition assembly;
        ModuleDefinition module;
        TypeReference irecyclableTypedef;
        TypeReference monobehaviourTyperef;

        int IWeaver.Priority => 2;

        public void Initialize(AssemblyDefinition assembly)
        {
            this.assembly = assembly;
            this.module = assembly.MainModule;
            this.irecyclableTypedef = module.ImportReference(typeof(Apkd.Internal.IRecyclable));
            this.monobehaviourTyperef = module.ImportReference(typeof(UnityEngine.MonoBehaviour));
        }

        readonly HashSet<(PropertyDefinition, FieldDefinition)> propertiesToValidate = new HashSet<(PropertyDefinition, FieldDefinition)>();

        public void ProcessAssembly()
        {
            IEnumerable<(TypeDefinition type, TypeReference iface)> GetTypes()
            {
                foreach (var type in module.Types)
                {
                    foreach (var iface in type.Interfaces.Where(x => x.InterfaceType.FullName.StartsWith("Apkd.Internal.IRecyclable`1")))
                    {
                        if (type.IsInterface)
                            continue;

                        if (type != (iface.InterfaceType as GenericInstanceType)?.GenericArguments.FirstOrDefault()?.Resolve())
                        {
                            UnityEngine.Debug.LogError($"Illegal <i>{iface.InterfaceType.FullName}</i> interface generic argument in <i>{type.FullName}</i>.");
                            continue;
                        }

                        yield return (type, iface.InterfaceType);
                    }
                }
            }
            
            foreach (var (type, iface) in GetTypes().ToArray())
            {
                var method = GetOrEmitPublicMethodWithBaseCall(type, "<codegen>Recycle");
                method.Parameters.Add(new ParameterDefinition(monobehaviourTyperef));
                var il = method.Body.GetILProcessor();
                var top = method.Body.Instructions[0];
                {
                    il.InsertBefore(top, Create(OpCodes.Ldarg_0));
                    il.InsertBefore(top, Create(OpCodes.Ldarg_0));
                    il.InsertBefore(top, Create(Call, type.Methods.First(x => x.Name == "Recycle" && x.Parameters[0].ParameterType == type)));
                }
                method.Overrides.Add(module.ImportReference(irecyclableTypedef.Resolve().Interfaces[0].InterfaceType.Resolve().Methods[0].MakeHostInstanceGeneric(monobehaviourTyperef)));
                type.Interfaces.Add(new InterfaceImplementation(irecyclableTypedef));
            }
        }

        static void Log(object obj) => UnityEngine.Debug.Log(obj);

        MethodDefinition EmitMethod(TypeDefinition type, string methodName, MethodAttributes attr = MethodAttributes.Private, TypeReference returnType = null)
        {
            returnType = returnType ?? module.TypeSystem.Void;
            var method = new MethodDefinition(
                methodName,
                attributes: MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                returnType);
            type.Methods.Add(method);
            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ret);
            return method;
        }

        MethodDefinition GetOrEmitPublicMethodWithBaseCall(TypeDefinition type, string methodName)
        {
            MethodDefinition EmitPrivateVoidMethodWithBaseCall()
            {
                var method = EmitMethod(type, methodName);
                var il = method.Body.GetILProcessor();

                var baseTypeDef = type.BaseType as TypeDefinition;
                var baseMethod = baseTypeDef?.Methods.FirstOrDefault(m => m.Name == methodName && m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void");
                if (baseMethod != null)
                {
                    var top = method.Body.Instructions[0];
                    il.InsertBefore(top, Create(OpCodes.Ldarg_0));
                    il.InsertBefore(top, Create(OpCodes.Call, baseMethod));
                }
                return method;
            }

            bool IsMethodDefinition(MethodDefinition m)
                => m.Name == methodName && m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void";

            return type.Methods.FirstOrDefault(IsMethodDefinition) ?? EmitPrivateVoidMethodWithBaseCall();
        }

        const System.Reflection.BindingFlags flags
            = System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Static;

        Instruction MethodCallInstruction(System.Reflection.MethodInfo methodInfo)
            => Create(OpCodes.Call, module.ImportReference(methodInfo));
    }
}