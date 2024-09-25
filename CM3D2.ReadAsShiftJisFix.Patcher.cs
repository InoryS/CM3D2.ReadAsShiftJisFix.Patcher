using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;


[assembly: AssemblyVersion("1.0.0.2")]
[assembly: AssemblyFileVersion("1.0.0.2")]
[assembly: AssemblyTitle("https://github.com/InoryS/CM3D2.ReadAsShiftJisFix.Patcher")]
[assembly: AssemblyProduct("CM3D2.ReadAsShiftJisFix.Patcher")]
[assembly: AssemblyCopyright("WTFPL")]
namespace CM3D2.ReadAsShiftJisFix.Patcher
{
    public static class ReadAsShiftJisFixPatcher
    {
        public static void Patch(AssemblyDefinition assembly)
        {
            // Get the NUty type
            TypeDefinition nUtyType = assembly.MainModule.GetType("NUty");
            if (nUtyType == null)
            {
                throw new Exception("Cannot find type NUty");
            }

            // Add the new method
            AddReadAsShiftJisFixedMethod(assembly.MainModule, nUtyType);

            // Redirect calls to the new method
            RedirectReadAsShiftJisCalls(assembly.MainModule, nUtyType);
        }


        private static void ReplaceReadAsShiftJisMethod(ModuleDefinition module, MethodDefinition method)
        {
            method.Body.SimplifyMacros();

            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.InitLocals = true;

            ILProcessor ilProcessor = method.Body.GetILProcessor();

            // Define local variable string result
            var stringType = module.ImportReference(typeof(string));
            var resultVariable = new VariableDefinition(stringType);
            method.Body.Variables.Add(resultVariable);

            // Load encoding 932 (Shift-JIS)
            var encodingType = module.ImportReference(typeof(System.Text.Encoding));
            MethodReference getEncodingMethod = module.ImportReference(
                typeof(System.Text.Encoding).GetMethod("GetEncoding", new Type[] { typeof(int) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, 932));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, getEncodingMethod));

            // Load parameter bArray
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));

            // Call Encoding.GetString method
            MethodReference getStringMethod = module.ImportReference(
                typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, getStringMethod));

            // Store the result in the local variable result
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, resultVariable));

            // Load result
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, resultVariable));

            // Create a character array containing '\0'
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4_1));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Newarr, module.ImportReference(typeof(char))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Dup));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4_0));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4_0));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stelem_I2));

            // Call the TrimEnd method
            MethodReference trimEndMethod = module.ImportReference(
                typeof(string).GetMethod("TrimEnd", new Type[] { typeof(char[]) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, trimEndMethod));

            // Return result
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

            method.Body.OptimizeMacros();
        }


        private static void AddReadAsShiftJisFixedMethod(ModuleDefinition module, TypeDefinition nUtyType)
        {
            // Define the method
            var method = new MethodDefinition("ReadAsShiftJisFixed",
                MethodAttributes.Public | MethodAttributes.Static,
                module.ImportReference(typeof(string)));

            // Add parameter: byte[] bArray
            method.Parameters.Add(new ParameterDefinition("bArray",
                ParameterAttributes.None, module.ImportReference(typeof(byte[]))));

            method.Body.InitLocals = true;
            ILProcessor ilProcessor = method.Body.GetILProcessor();

            // Load Encoding.GetEncoding(932)
            var getEncodingMethod = module.ImportReference(
                typeof(System.Text.Encoding).GetMethod("GetEncoding", new Type[] { typeof(int) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, 932));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, getEncodingMethod));

            // Load parameter bArray
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));

            // Call Encoding.GetString(byte[])
            var getStringMethod = module.ImportReference(
                typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, getStringMethod));

            // TrimEnd('\0')
            var trimEndMethod = module.ImportReference(
                typeof(string).GetMethod("TrimEnd", new Type[] { typeof(char[]) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4_1));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Newarr, module.ImportReference(typeof(char))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Dup));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4_0));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4_0)); // '\0' character
            ilProcessor.Append(ilProcessor.Create(OpCodes.Conv_U2));   // Convert to char
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stelem_I2));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, trimEndMethod));

            // Return the result
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

            method.Body.OptimizeMacros();

            // Add the method to NUty
            nUtyType.Methods.Add(method);
        }


        private static void RedirectReadAsShiftJisCalls(ModuleDefinition module, TypeDefinition nUtyType)
        {
            var originalMethod = PatcherHelper.GetMethod(nUtyType, "ReadAsShiftJis", "System.Byte[]");
            var newMethod = PatcherHelper.GetMethod(nUtyType, "ReadAsShiftJisFixed", "System.Byte[]");

            foreach (var type in module.Types)
            {
                RedirectMethodCalls(type, originalMethod, newMethod);
            }
        }

        private static void RedirectMethodCalls(TypeDefinition type, MethodDefinition originalMethod, MethodDefinition newMethod)
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;

                var ilProcessor = method.Body.GetILProcessor();
                var instructions = method.Body.Instructions;

                for (int i = 0; i < instructions.Count; i++)
                {
                    var inst = instructions[i];
                    if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt) &&
                        inst.Operand is MethodReference methodRef &&
                        methodRef.Resolve() == originalMethod)
                    {
                        inst.Operand = newMethod;
                    }
                }
            }

            foreach (var nestedType in type.NestedTypes)
            {
                RedirectMethodCalls(nestedType, originalMethod, newMethod);
            }
        }


        public static readonly string[] TargetAssemblyNames = new string[]
        {
            "Assembly-CSharp-firstpass.dll"
        };
    }
}
