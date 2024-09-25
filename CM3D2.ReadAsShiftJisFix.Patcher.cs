using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ReiPatcher;
using ReiPatcher.Patch;


[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyTitle("https://github.com/InoryS/CM3D2.ReadAsShiftJisFix.Patcher")]
[assembly: AssemblyProduct("CM3D2.ReadAsShiftJisFix.Patcher")]
[assembly: AssemblyCopyright("WTFPL")]
namespace CM3D2.ReadAsShiftJisFix.Patcher
{
    public class ReadAsShiftJisFixPatcher : PatchBase
    {
        public override string Name => "CM3D2.ReadAsShiftJisFix.Patcher";

        public override string Version => "1.0.0.1";

        public override void PrePatch()
        {
            RPConfig.RequestAssembly("Assembly-CSharp-firstpass.dll");
        }

        public override bool CanPatch(PatcherArguments args)
        {
            return args.Assembly.Name.Name == "Assembly-CSharp-firstpass"
                   && GetPatchedAttributes(args.Assembly).All(att => att.Info != Name);
        }

        public override void Patch(PatcherArguments args)
        {
            try
            {
                AssemblyDefinition assembly = args.Assembly;

                // Get the NUty type
                TypeDefinition nUtyType = assembly.MainModule.GetType("NUty");
                if (nUtyType == null)
                {
                    throw new Exception("ReadAsShiftJisFixPatcher: Can not find type NUty");
                }

                // Get the ReadAsShiftJis method
                MethodDefinition readAsShiftJisMethod =
                    PatcherHelper.GetMethod(nUtyType, "ReadAsShiftJis", new string[] { "System.Byte[]" });
                if (readAsShiftJisMethod == null)
                {
                    throw new Exception("ReadAsShiftJisFixPatcher: Can not find method ReadAsShiftJis");
                }

                // Modify the method body
                ReplaceReadAsShiftJisMethod(assembly.MainModule, readAsShiftJisMethod);

                // Mark the assembly as patched
                SetPatchedAttribute(args.Assembly, Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        // public override void PostPatch()
        // {}


        private void ReplaceReadAsShiftJisMethod(ModuleDefinition module, MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.InitLocals = true;

            ILProcessor ilProcessor = method.Body.GetILProcessor();

            // Define local variable string result
            var stringType = module.ImportReference(typeof(string));
            var resultVariable = new VariableDefinition(stringType);
            method.Body.Variables.Add(resultVariable);

            // Load encoding 932 (Shift-JIS),
            // CM3D2's mono does not support CodePage 932, so you need to import I18N.dll and I18N.CJK.dll
            MethodReference getEncodingMethod =
                module.ImportReference(
                    typeof(System.Text.Encoding).GetMethod("GetEncoding", new Type[] { typeof(int) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, 932));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, getEncodingMethod));

            // Load parameter bArray
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));

            // Call Encoding.GetString method
            MethodReference getStringMethod =
                module.ImportReference(
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
            ilProcessor.Append(ilProcessor.Create(OpCodes.Conv_U2));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stelem_I2));

            // Call the TrimEnd method
            MethodReference trimEndMethod =
                module.ImportReference(typeof(string).GetMethod("TrimEnd", new Type[] { typeof(char[]) }));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, trimEndMethod));

            // Return result
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));
        }
    }
}




// Original method reference
//
// public static string ReadAsShiftJis(byte[] bArray)
// {
//     StringBuilder stringBuilder = new StringBuilder(bArray.Length + 1);
//     NUty.MultiByteToWideChar(1U, 0U, bArray, -1, stringBuilder, stringBuilder.Capacity);
//     return stringBuilder.ToString();
// }