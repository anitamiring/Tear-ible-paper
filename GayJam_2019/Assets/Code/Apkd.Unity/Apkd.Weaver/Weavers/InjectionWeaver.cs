using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Mono.Cecil.Cil.Instruction;
using static UnityEngine.ParticleSystem;
using static Apkd.Internal.InjectHelpers;

namespace Apkd.Weaver
{
    sealed class InjectionWeaver : ILateWeaver
    {
        ModuleDefinition module;

        readonly HashSet<TypeDefinition> injectedRuntimeSetTypes = new HashSet<TypeDefinition>();
        readonly HashSet<TypeDefinition> injectedSingletonTypes = new HashSet<TypeDefinition>();

        int IWeaver.Priority => 1;

        public void Initialize(AssemblyDefinition assembly)
        {
            this.module = assembly.MainModule;
        }

        public void ProcessAssembly()
        {
            IEnumerable<(TypeDefinition, FieldDefinition, PropertyDefinition, CustomAttribute)> GetFields()
            {
                IEnumerable<(TypeDefinition, FieldDefinition, PropertyDefinition, CustomAttribute)> GetFieldsForType(TypeDefinition type)
                {
                    foreach (var property in type.Properties.Where(x => x.GetMethod?.IsCompilerGenerated() ?? false))
                    {
                        var attribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.StartsWith("Apkd.Inject"));
                        if (attribute != null)
                        {
                            property.CustomAttributes.Remove(attribute);
                            var backingField = property.Resolve()
                                .GetMethod.Body.Instructions
                                .Single(p => p.OpCode == OpCodes.Ldfld)
                                .Operand as FieldDefinition;

                            if (backingField.IsCompilerGenerated())
                            {
#if ODIN_INSPECTOR
                                backingField.AddAttribute<Sirenix.OdinInspector.ReadOnlyAttribute>(module);
#else
                                backingField.AddAttribute<ReadOnlyAttribute>(module);
#endif
                                yield return (type, backingField, property, attribute);
                            }
                        }
                    }
                    foreach (var field in type.Fields)
                    {
                        var attribute = field.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.StartsWith("Apkd.Inject"));
                        if (attribute != null)
                            yield return (type, field, null, attribute);
                    }
                }
                foreach (var type in module.Types)
                    foreach (var tuple in GetFieldsForType(type).DistinctBy(x => x.Item2))
                        yield return tuple;
            }

            foreach (var (type, field, property, attribute) in GetFields())
            {
                MethodDefinition GetAwakeMethod() => GetOrEmitPrivateMethodWithBaseCall(type, "Awake");
                MethodDefinition GetOnValidateMethod() => GetOrEmitPrivateMethodWithBaseCall(type, "OnValidate");
                MethodDefinition GetInitializationMethod() => GetOrEmitInitializationMethod(type, "<codegen>InitializeComponent", GetAwakeMethod());
                MethodDefinition GetInitializationMethodEditor() => GetOrEmitInitializationMethod(type, "<codegen>InitializeComponentEditor", GetOnValidateMethod());

                bool isSerialized = field.CustomAttributes.Any(x => x.Is<UnityEngine.SerializeField>());

                bool IsSingletonPropertyType(TypeReference p) =>
                    p.Resolve().IsInterface || p.DerivesFrom<UnityEngine.MonoBehaviour>();

                bool shouldUseSingletonPropertyReplacement = true
                    && property != null
                    && property.PropertyType.Module == module
                    && attribute.AttributeType.FullName == TypeName<Apkd.Inject.Singleton>.Value
                    && IsSingletonPropertyType(property.PropertyType);

#if UNITY_EDITOR && ODIN_INSPECTOR
                if (!field.FieldType.IsValueType)
                {
                    if (isSerialized)
                    {
                        {
                            var attr = field.AddAttribute<Sirenix.OdinInspector.SuffixLabelAttribute>(module);
                            attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "[S]"));
                            attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.Boolean, false));
                        }
                        if (!attribute.PropertyEquals(nameof(Inject.Optional), true))
                        {
                            var attr = field.AddAttribute<Sirenix.OdinInspector.RequiredAttribute>(module, x => x.GetParameters().Length == 1);
                            attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, $"{attribute.AttributeType.FullName.Replace('/', '.')} failed."));
                        }
                    }
                    else
                    {
                        field.AddAttribute<Sirenix.OdinInspector.ShowInInspectorAttribute>(module);
                        // field.AddAttribute<Sirenix.OdinInspector.HideInEditorModeAttribute>(module);
                    }
                    {
                        var attr = field.AddAttribute<Sirenix.OdinInspector.LabelTextAttribute>(module);
                        string name = UnityEditor.ObjectNames.NicifyVariableName(property?.Name ?? field.Name);
                        attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, name));
                    }
                    {
                        var attr = field.AddAttribute<Sirenix.OdinInspector.FoldoutGroupAttribute>(module, x => x.GetParameters().Length == 3);
                        attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "Injected Components"));
                        attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.Boolean, false));
                        attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.Int32, int.MinValue));
                    }
                }
#endif
                if (property == null && !field.FieldType.IsValueType && !field.IsCompilerGenerated())
                    UnityEngine.Debug.LogError($"Field <i>{field.FullName}</i> needs to be an auto-implemented property to use injection.");

                if (shouldUseSingletonPropertyReplacement)
                    ReplaceSingletonFieldReference(type, property, attribute);
                else if (isSerialized)
                    InsertInjectionCall(GetInitializationMethodEditor(), field, attribute);
                else
                    InsertInjectionCall(GetInitializationMethod(), field, attribute);
            }

            IEnumerable<(TypeDefinition injected, TypeDefinition resolved)> GetInterfaceTypePairs(IEnumerable<TypeDefinition> types)
            {
                foreach (var injected in types)
                {
                    if (injected.IsInterface)
                        foreach (var resolved in module.Types
                                .Where(t => t.Interfaces
                                    .Select(x => x.InterfaceType)
                                    .Where(x => x.Module == module)
                                    .Contains(injected)))
                            yield return (injected, resolved);
                    else
                        yield return (injected, injected);
                }
            }

            foreach (var (injected, resolved) in GetInterfaceTypePairs(injectedRuntimeSetTypes))
            {
                var onEnableMethod = GetOrEmitPrivateMethodWithBaseCall(resolved, "OnEnable");
                var onDisableMethod = GetOrEmitPrivateMethodWithBaseCall(resolved, "OnDisable");
                InsertContainerInstanceCall(onEnableMethod, injected, containerType: typeof(Internal.SetContainer<>), methodName: "RegisterInstance");
                InsertContainerInstanceCall(onDisableMethod, injected, containerType: typeof(Internal.SetContainer<>), methodName: "UnregisterInstance");
            }

            foreach (var (injected, resolved) in GetInterfaceTypePairs(injectedSingletonTypes))
            {
                var onEnableMethod = GetOrEmitPrivateMethodWithBaseCall(resolved, "OnEnable");
                var onDisableMethod = GetOrEmitPrivateMethodWithBaseCall(resolved, "OnDisable");
                InsertContainerInstanceCall(onEnableMethod, injected, containerType: typeof(Internal.SingletonContainer<>), methodName: "RegisterInstance");
                InsertContainerInstanceCall(onDisableMethod, injected, containerType: typeof(Internal.SingletonContainer<>), methodName: "UnregisterInstance");
            }
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

        MethodDefinition GetOrEmitInitializationMethod(TypeDefinition type, string methodName, MethodDefinition callSite)
        {
            var method = type.Methods.FirstOrDefault(m => m.Name == methodName);
            if (method != null)
                return method;

            method = EmitMethod(type, methodName);
            InsertMethodCall(targetMethod: method, sourceMethod: callSite);
            method.IsCompilerControlled = true;
            return method;
        }

        // MethodDefinition GetBaseMethod(TypeDefinition type, Func<MethodDefinition, bool> filter)
        // {
        //     MethodDefinition result;
        //     while ((type = type.BaseType?.Resolve()) != null)
        //         if ((result = type.Methods.FirstOrDefault(filter)) != null)
        //             return result;
        //     return null;
        // }

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

        void WrapBlockInTryCatch(ILProcessor il, Instruction start, Instruction next)
        {
            var leaveCatch = Create(OpCodes.Leave, next);
            var leaveTry = Create(OpCodes.Leave, next);
            var ldthis = Create(OpCodes.Ldarg_0);
            var logException = MethodCallInstruction(typeof(UnityEngine.Debug).GetMethod("LogException", new[] { typeof(System.Exception), typeof(UnityEngine.Object) }));
            il.InsertBefore(next, leaveTry);
            il.InsertBefore(next, ldthis);
            il.InsertBefore(next, logException);
            il.InsertBefore(next, leaveCatch);
            var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = start,
                TryEnd = ldthis,
                HandlerStart = ldthis,
                HandlerEnd = next,
                CatchType = module.ImportReference(typeof(System.Exception)),
            };
            il.Body.ExceptionHandlers.Add(handler);
        }

        // void WrapMethodInTryCatch(MethodDefinition method)
        // {
        //     var il = method.Body.GetILProcessor();
        //     var top = method.Body.Instructions.First();
        //     var ret = method.Body.Instructions.Last();
        //     var leaveTry = Create(OpCodes.Leave, ret);
        //     var leaveCatch = Create(OpCodes.Leave, ret);
        //     var ldthis = Create(OpCodes.Ldarg_0);
        //     var logException = MethodCallInstruction(typeof(UnityEngine.Debug).GetMethod(nameof(UnityEngine.Debug.LogException), new[] { typeof(System.Exception), typeof(UnityEngine.Object) }));
        //     il.InsertBefore(ret, leaveTry);
        //     il.InsertBefore(ret, ldthis);
        //     il.InsertBefore(ret, logException);
        //     il.InsertBefore(ret, leaveCatch);
        //     var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
        //     {
        //         TryStart = top,
        //         TryEnd = ldthis,
        //         HandlerStart = ldthis,
        //         HandlerEnd = ret,
        //         CatchType = module.ImportReference(typeof(System.Exception)),
        //     };
        //     il.Body.ExceptionHandlers.Add(handler);
        // }

        void InsertInjectionCall(MethodDefinition method, FieldDefinition field, CustomAttribute attribute)
        {
            var il = method.Body.GetILProcessor();
            var top = method.Body.Instructions[0];
            bool IsOptional = attribute.PropertyEquals(key: nameof(Inject.Optional), value: true);

            TypeReference UnwrapArrayType(TypeReference type)
                => type.IsArray ? type.GetElementType() : type;

            var elementType = field.FieldType;
            var (resolveMethodName, useGenericMethodCall, canBeSerialized) = GetNameOfResolveMethodInfo(ref elementType, attribute);
            elementType = UnwrapArrayType(elementType);

            var hasSerializeFieldAttribute = field.HasAttribute<UnityEngine.SerializeField>();

            if (!canBeSerialized && hasSerializeFieldAttribute)
                throw new System.NotSupportedException($"Field of type {field.FieldType.Name} marked with [{attribute.AttributeType.Name}] should not be serialized.");

            if (hasSerializeFieldAttribute)
#if ODIN_INSPECTOR
                field.AddAttribute<Sirenix.OdinInspector.ReadOnlyAttribute>(module);
#else
                field.AddAttribute<ReadOnlyAttribute>(module);
#endif

            Instruction first, last;
            {
                il.InsertBefore(top, first = Create(OpCodes.Ldarg_0));
                // store field
                {
                    il.InsertBefore(top, Create(OpCodes.Ldarg_0));
                    if (useGenericMethodCall)
                    {
                        il.InsertBefore(top, Create(OpCodes.Ldarg_0));
                        il.InsertBefore(top, Create(OpCodes.Call, module.ImportReference(typeof(Internal.InjectHelpers).GetMethod(resolveMethodName, flags)).MakeGenericInstance(elementType)));
                    }
                    else
                    {
                        // load dependency resolution result
                        {
                            il.InsertBefore(top, Create(OpCodes.Ldarg_0));
                            // load type
                            {
                                il.InsertBefore(top, Create(OpCodes.Ldtoken, module.ImportReference(elementType)));
                                il.InsertBefore(top, MethodCallInstruction(typeof(System.Type).GetMethod(nameof(System.Type.GetTypeFromHandle), flags)));
                            }
                            il.InsertBefore(top, MethodCallInstruction(typeof(Internal.InjectHelpers).GetMethod(resolveMethodName, flags)));
                        }
                        if (!field.FieldType.IsValueType)
                        {
                            bool isInterface; // enable interface serialization
                            try
                            {
                                isInterface = field.FieldType.Resolve().IsInterface;
                            }
                            catch
                            {
                                isInterface = false;
                            }
                            if (isInterface)
                                il.InsertBefore(top, Create(OpCodes.Isinst, module.ImportReference(typeof(UnityEngine.Object))));
                            else
                                il.InsertBefore(top, Create(OpCodes.Isinst, field.FieldType));
                        }
                    }
                    il.InsertBefore(top, Create(OpCodes.Stfld, field));
                }
                // no-op (jump label)
                il.InsertBefore(top, last = Create(OpCodes.Nop));
                if (!IsOptional && !field.FieldType.IsValueType)
                {
                    il.InsertBefore(last, Create(OpCodes.Ldfld, field));
                    il.InsertBefore(last, Create(OpCodes.Brtrue_S, last));
                    il.InsertBefore(last, Create(OpCodes.Ldstr,
                        FormatLog($"Failed to initialize @*|{field.FieldType.Name} {field.Name}|*@ in component @*|{method.DeclaringType.FullName}|*@.\n|Component injection using *[{attribute.AttributeType.FullName.Replace('/', '.')}]*|"))
                    );
                    il.InsertBefore(last, Create(OpCodes.Ldarg_0));
                    if (hasSerializeFieldAttribute)
                        il.InsertBefore(last, MethodCallInstruction(typeof(UnityEngine.Debug).GetMethod(nameof(UnityEngine.Debug.LogError), new[] { typeof(string), typeof(UnityEngine.Object) })));
                    else
                        il.InsertBefore(last, MethodCallInstruction(typeof(UnityEngine.Debug).GetMethod(nameof(UnityEngine.Debug.LogWarning), new[] { typeof(string), typeof(UnityEngine.Object) })));
                }
            }

            // isdirty editor injection guard
            if (hasSerializeFieldAttribute)
            {
                il.InsertBefore(first, Create(OpCodes.Ldarg_0));
                il.InsertBefore(first, MethodCallInstruction(typeof(Internal.InjectHelpers).GetMethod(nameof(ShouldUpdateInjectedComponents), flags)));
                il.InsertBefore(first, Create(OpCodes.Brfalse, last));
            }

            WrapBlockInTryCatch(il, first, last);
        }

        // FieldReference GetGenericTypeFieldReference(TypeReference container, string fieldName)
        // {
        //     var field = module.ImportReference(container.Resolve().Fields.Single(x => x.Name == fieldName));
        //     field.DeclaringType = container;
        //     return module.ImportReference(new FieldReference(field.Name, module.ImportReference(field.FieldType), container));
        // }

        MethodReference GetGenericTypeMethodReference(TypeReference type, string methodName)
        {
            var method = module.ImportReference(type.Resolve().Methods.Single(x => x.Name == methodName));
            method.DeclaringType = method.DeclaringType.MakeGenericInstance((type as GenericInstanceType).GenericArguments[0]);
            return method;
        }

        void InsertContainerInstanceCall(MethodDefinition method, TypeDefinition type, System.Type containerType, string methodName)
        {
            var il = method.Body.GetILProcessor();
            Instruction top = method.Body.Instructions[0];
            var containerTypeReference = module.ImportReference(module.ImportReference(containerType).MakeGenericInstance(type));
            var registerMethod = GetGenericTypeMethodReference(containerTypeReference, methodName);
            il.InsertBefore(top, Create(OpCodes.Ldarg_0));
            il.InsertBefore(top, Create(OpCodes.Callvirt, registerMethod));
        }

        void ReplaceSingletonFieldReference(TypeDefinition type, PropertyDefinition property, CustomAttribute attribute)
        {
            injectedSingletonTypes.Add(property.PropertyType.Resolve());
            bool isOptional = attribute.PropertyEquals(key: nameof(Inject.Optional), value: true);
            string getInstanceMethodName = isOptional ? "GetInstanceNoWarn" : "GetInstance";
            var getMethod = property.GetMethod;
            var ldfld = getMethod.Body.Instructions.Single(x => x.OpCode == OpCodes.Ldfld);
            type.Fields.Remove((ldfld.Operand as FieldReference).Resolve());
            var containerTypeReference = module
                .ImportReference(module.ImportReference(typeof(Internal.SingletonContainer<>))
                .MakeGenericInstance(property.PropertyType));
            var methodReference = GetGenericTypeMethodReference(containerTypeReference, getInstanceMethodName);

            var il = getMethod.Body.GetILProcessor();
            il.InsertBefore(ldfld, Create(OpCodes.Pop));
            il.Replace(ldfld, Create(OpCodes.Call, methodReference));
        }

        static string FormatLog(string message)
        {
            int IndexOf(System.Text.StringBuilder b, char character)
            {
                for (int i = 0; i < b.Length; ++i)
                    if (b[i] == character) return i;
                return -1;
            }
            void StringInsertTag(System.Text.StringBuilder b, char character, string left, string right)
            {
                int i;
                bool even = false;
                while ((i = IndexOf(b, character)) >= 0)
                {
                    even = !even;
                    var token = even ? left : right;
                    b[i] = token[0];
                    b.Insert(i + 1, token.Substring(1));
                }
            }
            var builder = new System.Text.StringBuilder(message);
            StringInsertTag(builder, '*', "<i>", "</i>");
            StringInsertTag(builder, '@', "<color=#00003399>", "</color>");
            StringInsertTag(builder, '|', "<size=9>", "</size>");
            return builder.ToString();
        }

        static void InsertMethodCall(MethodDefinition targetMethod, MethodDefinition sourceMethod, Instruction sourceSite = null)
        {
            sourceSite = sourceSite ?? sourceMethod.Body.Instructions.First();
            var il = sourceMethod.Body.GetILProcessor();

            il.InsertBefore(sourceSite, Create(OpCodes.Ldarg_0));
            il.InsertBefore(sourceSite, Create(OpCodes.Call, targetMethod));
        }

        static void Log(object obj) => UnityEngine.Debug.Log(obj);

        (string methodName, bool generic, bool serializable) GetNameOfResolveMethodInfo(ref TypeReference type, CustomAttribute attr)
        {
            string attrName = attr.AttributeType.FullName;
            string typeName = type.FullName;

            if (attrName == TypeName<Apkd.Inject>.Value)
            {
                if (typeName.StartsWith("Apkd.ReadOnlySet`1") && type is GenericInstanceType set)
                {
                    var innerType = set.GenericArguments.Single().Resolve();

                    if (!innerType.IsInterface && !innerType.DerivesFrom<UnityEngine.MonoBehaviour>())
                        throw new NotSupportedException("Injected ComponentSet generic argument must inherit from MonoBehaviour or be an interface");

                    if (innerType.Module != module)
                        throw new NotSupportedException("Injected ComponentSet generic argument must be a MonoBehaviour defined in main assembly");

                    injectedRuntimeSetTypes.Add(innerType);
                    type = innerType;
                    return (nameof(ResolveRuntimeSet), generic: true, serializable: false);
                }

                if (type.FullName.StartsWith("UnityEngine.ParticleSystem/"))
                {
                    if (type.IsArray)
                        throw new NotSupportedException($"Array injection not supported in {attrName}");

                    if (typeName == TypeName<CollisionModule>.Value)
                        return (nameof(ResolveCollisionModule), false, false);

                    if (typeName == TypeName<ColorBySpeedModule>.Value)
                        return (nameof(ResolveColorBySpeedModule), false, false);

                    if (typeName == TypeName<ColorOverLifetimeModule>.Value)
                        return (nameof(ResolveColorOverLifetimeModule), false, false);

                    if (typeName == TypeName<CustomDataModule>.Value)
                        return (nameof(ResolveCustomDataModule), false, false);

                    if (typeName == TypeName<EmissionModule>.Value)
                        return (nameof(ResolveEmissionModule), false, false);

                    if (typeName == TypeName<ExternalForcesModule>.Value)
                        return (nameof(ResolveExternalForcesModule), false, false);

                    if (typeName == TypeName<ForceOverLifetimeModule>.Value)
                        return (nameof(ResolveForceOverLifetimeModule), false, false);

                    if (typeName == TypeName<InheritVelocityModule>.Value)
                        return (nameof(ResolveFromheritVelocityModule), false, false);

                    if (typeName == TypeName<LightsModule>.Value)
                        return (nameof(ResolveLightsModule), false, false);

                    if (typeName == TypeName<LimitVelocityOverLifetimeModule>.Value)
                        return (nameof(ResolveLimitVelocityOverLifetimeModule), false, false);

                    if (typeName == TypeName<MainModule>.Value)
                        return (nameof(ResolveMainModule), false, false);

                    if (typeName == TypeName<NoiseModule>.Value)
                        return (nameof(ResolveNoiseModule), false, false);

                    if (typeName == TypeName<RotationBySpeedModule>.Value)
                        return (nameof(ResolveRotationBySpeedModule), false, false);

                    if (typeName == TypeName<RotationOverLifetimeModule>.Value)
                        return (nameof(ResolveRotationOverLifetimeModule), false, false);

                    if (typeName == TypeName<ShapeModule>.Value)
                        return (nameof(ResolveShapeModule), false, false);

                    if (typeName == TypeName<SizeBySpeedModule>.Value)
                        return (nameof(ResolveSizeBySpeedModule), false, false);

                    if (typeName == TypeName<SizeOverLifetimeModule>.Value)
                        return (nameof(ResolveSizeOverLifetimeModule), false, false);

                    if (typeName == TypeName<SubEmittersModule>.Value)
                        return (nameof(ResolveSubEmittersModule), false, false);

                    if (typeName == TypeName<TextureSheetAnimationModule>.Value)
                        return (nameof(ResolveTextureSheetAnimationModule), false, false);

                    if (typeName == TypeName<TrailModule>.Value)
                        return (nameof(ResolveTrailModule), false, false);

                    if (typeName == TypeName<TriggerModule>.Value)
                        return (nameof(ResolveTriggerModule), false, false);

                    if (typeName == TypeName<VelocityOverLifetimeModule>.Value)
                        return (nameof(ResolveVelocityOverLifetimeModule), false, false);

                    throw new NotImplementedException($"Unsupported ParticleSystem.Module type: {type.FullName}");
                }

                return type.IsArray ? (nameof(ResolveArray), generic: true, serializable: true) : (nameof(Resolve), generic: false, serializable: true);
            }

            if (attrName == TypeName<Inject.FromChildren>.Value)
            {
                if (type.IsArray)
                {
                    if (attr.PropertyEquals(key: nameof(Inject.FromParents.IncludeInactive), value: true))
                        return (nameof(ResolveFromChildrenArrayIncludeInactive), generic: true, serializable: true);
                    else
                        return (nameof(ResolveFromChildrenArray), generic: true, serializable: true);
                }
                else
                {
                    if (attr.PropertyEquals(key: nameof(Inject.FromParents.IncludeInactive), value: true))
                        return (nameof(ResolveFromChildrenIncludeInactive), generic: false, serializable: true);
                    else
                        return (nameof(ResolveFromChildren), generic: false, serializable: true);
                }
            }
            if (attrName == TypeName<Inject.FromParents>.Value)
            {
                if (type.IsArray)
                {
                    if (attr.PropertyEquals(key: nameof(Inject.FromParents.IncludeInactive), value: true))
                        return (nameof(ResolveFromParentsArrayIncludeInactive), generic: true, serializable: true);
                    else
                        return (nameof(ResolveFromParentsArray), generic: true, serializable: true);
                }
                else
                {
                    if (attr.PropertyEquals(key: nameof(Inject.FromParents.IncludeInactive), value: true))
                    {
                        UnityEngine.Debug.LogError($"IncludeInactive is not supported with {TypeName<Inject.FromParents>.Value}");
                        return (nameof(ResolveFromParents), generic: false, serializable: true);
                    }
                    else
                    {
                        return (nameof(ResolveFromParents), generic: false, serializable: true);
                    }
                }
            }

            if (attrName == TypeName<Inject.Singleton>.Value)
            {
                if (type.Is<UnityEngine.Camera>())
                    return (nameof(ResolveMainCamera), generic: false, serializable: true);

                if (type.DerivesFrom<UnityEngine.ScriptableObject>())
                    return (nameof(ResolveScriptableObject), generic: false, serializable: true);

                var resolvedType = type.Resolve();
                if (resolvedType.Module == module)
                    injectedSingletonTypes.Add(resolvedType);

                return (nameof(ResolveSingleton), generic: true, serializable: false);
            }

            throw new NotImplementedException($"Unknown injection attribute: {attrName}");
        }

        const System.Reflection.BindingFlags flags
            = System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Static;

        Instruction MethodCallInstruction(System.Reflection.MethodInfo methodInfo)
            => Create(OpCodes.Call, module.ImportReference(methodInfo));
    }
}