using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swap.Modularity.Abstractions;

namespace Swap.Modularity.Tests;

internal static class ModuleAssemblyBuilder
{
    internal sealed record ModuleSpec(string Name, string[] DependsOn, bool LogCalls = true);

    public static Assembly Build(params ModuleSpec[] specs)
    {
        var an = new AssemblyName($"DynamicModules_{Guid.NewGuid():N}");
        var ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
        var mb = ab.DefineDynamicModule("Main");
        var iModule = typeof(IModule);
        var iServiceCollection = typeof(IServiceCollection);
        var iConfiguration = typeof(IConfiguration);
        var iEndpointRouteBuilder = typeof(IEndpointRouteBuilder);

        foreach (var spec in specs)
        {
            var tb = mb.DefineType($"{spec.Name}Module", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
            tb.AddInterfaceImplementation(iModule);

            // Property: string Name { get; }
            var nameProp = tb.DefineProperty("Name", PropertyAttributes.None, typeof(string), null);
            var getName = tb.DefineMethod("get_Name", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName, typeof(string), Type.EmptyTypes);
            var il = getName.GetILGenerator();
            il.Emit(OpCodes.Ldstr, spec.Name);
            il.Emit(OpCodes.Ret);
            nameProp.SetGetMethod(getName);
            tb.DefineMethodOverride(getName, iModule.GetProperty("Name")!.GetGetMethod()!);

            // Property: IReadOnlyList<string> DependsOn { get; }
            var dependsProp = tb.DefineProperty("DependsOn", PropertyAttributes.None, typeof(IReadOnlyList<string>), null);
            var getDepends = tb.DefineMethod("get_DependsOn", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName, typeof(IReadOnlyList<string>), Type.EmptyTypes);
            var ild = getDepends.GetILGenerator();
            ild.Emit(OpCodes.Ldc_I4, spec.DependsOn.Length);
            ild.Emit(OpCodes.Newarr, typeof(string));
            for (int i = 0; i < spec.DependsOn.Length; i++)
            {
                ild.Emit(OpCodes.Dup);
                ild.Emit(OpCodes.Ldc_I4, i);
                ild.Emit(OpCodes.Ldstr, spec.DependsOn[i]);
                ild.Emit(OpCodes.Stelem_Ref);
            }
            ild.Emit(OpCodes.Ret);
            dependsProp.SetGetMethod(getDepends);
            tb.DefineMethodOverride(getDepends, iModule.GetProperty("DependsOn")!.GetGetMethod()!);

            // Method: void ConfigureServices(IServiceCollection, IConfiguration)
            var confServices = tb.DefineMethod("ConfigureServices", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new[] { iServiceCollection, iConfiguration });
            var ils = confServices.GetILGenerator();
            if (spec.LogCalls)
            {
                // Call CallLog.Entries.Enqueue($"services:{Name}")
                var entriesProp = typeof(CallLog).GetField("Entries")!;
                var enqueue = entriesProp.FieldType.GetMethod("Enqueue")!;
                ils.Emit(OpCodes.Ldsfld, entriesProp);
                ils.Emit(OpCodes.Ldstr, $"services:{spec.Name}");
                ils.Emit(OpCodes.Callvirt, enqueue);
            }
            ils.Emit(OpCodes.Ret);
            tb.DefineMethodOverride(confServices, iModule.GetMethod("ConfigureServices")!);

            // Method: void ConfigureEndpoints(IEndpointRouteBuilder)
            var confEndpoints = tb.DefineMethod("ConfigureEndpoints", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new[] { iEndpointRouteBuilder });
            var ile = confEndpoints.GetILGenerator();
            if (spec.LogCalls)
            {
                var entriesProp2 = typeof(CallLog).GetField("Entries")!;
                var enqueue2 = entriesProp2.FieldType.GetMethod("Enqueue")!;
                ile.Emit(OpCodes.Ldsfld, entriesProp2);
                ile.Emit(OpCodes.Ldstr, $"endpoints:{spec.Name}");
                ile.Emit(OpCodes.Callvirt, enqueue2);
            }
            ile.Emit(OpCodes.Ret);
            tb.DefineMethodOverride(confEndpoints, iModule.GetMethod("ConfigureEndpoints")!);

            // Optional: void ConfigureEventChains(IEventChainRegistrar) - no-op
            var confChains = tb.DefineMethod("ConfigureEventChains", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new[] { typeof(object) /* any registrar */ });
            var ilc = confChains.GetILGenerator();
            ilc.Emit(OpCodes.Ret);

            tb.CreateType();
        }

        return ab;
    }
}
