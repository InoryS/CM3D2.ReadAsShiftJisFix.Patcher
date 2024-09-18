// https://github.com/neguse11/cm3d2_plugins_okiba/blob/master/Lib/PatcherHelper.cs
// https://github.com/neguse11/cm3d2_plugins_okiba/blob/master/LICENSE
// WTFPL
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PatcherHelper
{
    public delegate void InsertInstDelegate(Instruction newInst);

    public static MethodDefinition GetMethod(TypeDefinition type, string methodName)
    {
        return type.Methods.FirstOrDefault(m => m.Name == methodName);
    }

    public static void DumpMethods(TextWriter tw, TypeDefinition type)
    {
        foreach (MethodDefinition m in type.Methods)
        {
            tw.Write("{0} . {1}(", type.Name, m.Name);
            bool b = true;
            foreach (var p in m.Parameters)
            {
                if (!b)
                {
                    tw.Write(",");
                }

                b = false;
                tw.Write("{0}", p.ParameterType.FullName);
            }

            tw.WriteLine(")");
        }
    }

    public static MethodDefinition GetMethod(TypeDefinition type, string methodName, params string[] args)
    {
        if (args == null)
        {
            return GetMethod(type, methodName);
        }

        for (int i = 0; i < type.Methods.Count; i++)
        {
            MethodDefinition m = type.Methods[i];
            if (m.Name == methodName && m.Parameters.Count == args.Length)
            {
                bool b = true;
                for (int j = 0; j < args.Length; j++)
                {
                    if (m.Parameters[j].ParameterType.FullName != args[j])
                    {
                        b = false;
                        break;
                    }
                }

                if (b)
                {
                    return m;
                }
            }
        }

        return null;
    }

}