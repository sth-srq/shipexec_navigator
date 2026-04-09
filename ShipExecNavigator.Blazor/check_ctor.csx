using System;
using System.Reflection;
var asm = Assembly.LoadFrom(@""C:\Users\Admin\.nuget\packages\icsharpcode.decompiler\10.0.0.8330\lib\net8.0\ICSharpCode.Decompiler.dll"");
var type = asm.GetType(""ICSharpCode.Decompiler.CSharp.ProjectDecompiler.WholeProjectDecompiler"");
if (type == null) { Console.WriteLine(""Type not found in net8.0""); return; }
foreach (var c in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
    var parms = string.Join("", "", Array.ConvertAll(c.GetParameters(), p => p.ParameterType.FullName + "" "" + p.Name));
    Console.WriteLine(""ctor("" + parms + "")"");
}
