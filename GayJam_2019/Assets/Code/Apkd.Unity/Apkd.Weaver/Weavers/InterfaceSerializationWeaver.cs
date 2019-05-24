using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static System.Reflection.BindingFlags;
using static Mono.Cecil.Cil.Instruction;

namespace Apkd.Weaver
{
    sealed class InterfaceSerializationWeaver : ILateWeaver
    {
        AssemblyDefinition assembly;
        ModuleDefinition module;
        TypeDefinition[] allTypes;
        PropertyDefinition[] allProperties;

        int IWeaver.Priority => 2;

        public void Initialize(AssemblyDefinition assembly)
        {
            this.assembly = assembly;
            this.module = assembly.MainModule;

            this.allTypes = module.Types
                .SelectMany(x => x.NestedTypes)
                .Concat(module.Types)
                .ToArray();

            this.allProperties = allTypes
                .SelectMany(x => x.Properties)
                .ToArray();
        }

        readonly HashSet<(PropertyDefinition, FieldDefinition)> propertiesToValidate = new HashSet<(PropertyDefinition, FieldDefinition)>();

        public void ProcessAssembly()
        {
            // allow serialization of interfaces
            // replaces backing field type for serialized readonly auto-implemented properties with UnityEngine.Object
            var properties = allProperties
                .Where(p => p.SetMethod == null)
                .Where(p => p.GetMethod.IsCompilerGenerated())
                .Select(p => (property: p, field: p.GetBackingField()))
                .Where(p => p.field != null)
                .Where(p => p.field.HasAttribute<UnityEngine.SerializeField>()).ToArray();

            foreach (var (property, backingField) in properties)
            {
                var isInterface = backingField.FieldType.Resolve().IsInterface;
                var isArray = backingField.FieldType.IsArray;

                if (isInterface)
                {
                    var targetType = module.ImportReference(isArray ? typeof(UnityEngine.Object[]) : typeof(UnityEngine.Object));
                    backingField.FieldType = targetType;

                    // if (!isArray)
                    // {
                    //     var implementations = GetInterfaceImplementations(property.PropertyType);
                    //     if (implementations.Length > 0)
                    //     {
                    //         string filter = GetInterfaceImplementations(property.PropertyType)
                    //             .Aggregate(
                    //                 seed: new System.Text.StringBuilder(capacity: 32),
                    //                 func: (l, r) => l.Append("t:").Append(r.Name).Append(' '))
                    //             .ToString();

                    //         var attributeProperties = backingField
                    //             .AddAttribute<Internal.CustomObjectPickerAttribute>(module)
                    //             .Properties;

                    //         attributeProperties.Add(new CustomAttributeNamedArgument(
                    //             name: nameof(Internal.CustomObjectPickerAttribute.InterfaceType),
                    //             argument: new CustomAttributeArgument(module.ImportReference(typeof(System.Type)), property.PropertyType)));
                    //     }
                    // }

                    // var instr = property
                    //     ?.GetMethod
                    //     ?.Body
                    //     ?.Instructions
                    //     ?.FirstOrDefault(x => x.OpCode == OpCodes.Ldfld);

                    // var il = property.GetMethod.Body.GetILProcessor();
                    // il.InsertBefore(instr, Instruction.Create(OpCodes.Ldflda, instr.Operand as FieldReference));
                    // il.InsertBefore(instr, Instruction.Create(OpCodes.Ldind_Ref));
                    // il.Remove(instr);

#if UNITY_EDITOR && ODIN_INSPECTOR
                    // hack: hide injected interface array preview because odin can't display this field
                    if (backingField.FieldType.IsArray && backingField.HasAttribute(x => x.AttributeType.FullName.StartsWith("Apkd.Inject")))
                    {
                        backingField.AddAttribute<UnityEngine.HideInInspector>(module);
                    }
                    else
                    {
                        // display property using odin instead of rendering the standard unity picker
                        foreach (var attr in backingField.CustomAttributes.Where(x => x.AttributeType.FullName.StartsWith(nameof(Sirenix))))
                            property.CustomAttributes.Add(attr);
                        property.AddAttribute<Sirenix.OdinInspector.ShowInInspectorAttribute>(module);
                        backingField.AddAttribute<UnityEngine.HideInInspector>(module);
                        backingField.RemoveAttribute<Sirenix.OdinInspector.ShowInInspectorAttribute>();
                    }
#endif

                    propertiesToValidate.Add((property, backingField));
                }
            }

            // emit validation
            foreach (var (property, backingField) in propertiesToValidate)
            {
                var onValidateMethod = GetOrEmitPrivateMethodWithBaseCall(property.DeclaringType, "OnValidate");
                EmitSerializedInterfaceValidation(onValidateMethod, property, backingField);
            }
        }

        readonly Dictionary<TypeReference, TypeReference[]> interfaceImplementationsCache = new Dictionary<TypeReference, TypeReference[]>();
        TypeReference[] GetInterfaceImplementations(TypeReference interfaceType)
        {
            TypeReference[] result;
            if (interfaceImplementationsCache.TryGetValue(interfaceType, out result))
                return result;

            interfaceImplementationsCache[interfaceType] = result = allTypes.Where(t => t.Interfaces.Any(i => i.InterfaceType == interfaceType)).ToArray();
            return result;
        }

        MethodDefinition EmitMethod(TypeDefinition type, string methodName, MethodAttributes attr = MethodAttributes.Private, TypeReference returnType = null)
        {
            returnType = returnType ?? module.TypeSystem.Void;
            var method = new MethodDefinition(methodName, MethodAttributes.Private, returnType);
            type.Methods.Add(method);
            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ret);
            return method;
        }

        MethodDefinition GetOrEmitPrivateMethodWithBaseCall(TypeDefinition type, string methodName)
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

        void EmitSerializedInterfaceValidation(MethodDefinition onValidateMethod, PropertyDefinition property, FieldDefinition backingField)
        {
            var il = onValidateMethod.Body.GetILProcessor();
            var top = onValidateMethod.Body.Instructions[0];
            var isArray = backingField.FieldType.IsArray;
            il.InsertBefore(top, Create(OpCodes.Ldarg_0));
            il.InsertBefore(top, Create(OpCodes.Ldflda, backingField));
            il.InsertBefore(top, Create(OpCodes.Ldtoken, module.ImportReference(property.PropertyType.Resolve())));
            il.InsertBefore(top, MethodCallInstruction(typeof(System.Type).GetMethod(nameof(System.Type.GetTypeFromHandle), flags)));
            if (isArray)
                il.InsertBefore(top, MethodCallInstruction(typeof(Internal.ValidationHelpers).GetMethod(nameof(Internal.ValidationHelpers.ValidateSerializedInterfaceArray))));
            else
                il.InsertBefore(top, MethodCallInstruction(typeof(Internal.ValidationHelpers).GetMethod(nameof(Internal.ValidationHelpers.ValidateSerializedInterface))));
        }

        const System.Reflection.BindingFlags flags
            = System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Static;

        Instruction MethodCallInstruction(System.Reflection.MethodInfo methodInfo)
            => Create(OpCodes.Call, module.ImportReference(methodInfo));
    }
}