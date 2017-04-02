using Microsoft.CSharp;
using Stef.DatabaseQuery.Business.Exceptions;
using Stef.DatabaseQuery.Business.Managers.CSharps;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class CSharpManager
    {
        private static object _SyncLock = new object();
        private static CSharpManager _Instance;

        private CSharpManager()
        {
        }

        public static CSharpManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new CSharpManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public Assembly CreateRuntimeAssembly(string classCode, bool throwExceptionOnCompileError)
        {
            if (String.IsNullOrEmpty(classCode))
                throw new ArgumentException("classCode is null or empty.", "classCode");

            var providerOptions = new Dictionary<string, string>();
            providerOptions.Add("CompilerVersion", "v4.0");

            using (CodeDomProvider compiler = new CSharpCodeProvider(providerOptions))
            {
                CompilerParameters compilerParams = GetCompilerParameters();
                CompilerResults compilerResults = compiler.CompileAssemblyFromSource(compilerParams, classCode);

                if (compilerResults.Errors.Count == 0)
                {
                    return compilerResults.CompiledAssembly;
                }
                else
                {
                    if (throwExceptionOnCompileError)
                        throw GetRuntimeAssemblyCompileException(classCode, compilerResults, compilerParams);

                    return null;
                }
            }
        }
        private CompilerParameters GetCompilerParameters()
        {
            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.GenerateInMemory = true;
            compilerParams.GenerateExecutable = false;
            compilerParams.IncludeDebugInformation = true;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Dictionary<string, string> refAssemblyDic = new Dictionary<string, string>();

            foreach (Assembly assembly in assemblies)
            {
                if (assembly.IsDynamic)
                    continue;
                if (string.IsNullOrEmpty(assembly.Location))
                    continue;

                if (assembly.FullName.StartsWith("System"))
                {
                    refAssemblyDic[Path.GetFileName(assembly.Location.ToLower())] = assembly.Location;
                }
            }

            refAssemblyDic
                .Values
                .ToList()
                .ForEach(c => compilerParams.ReferencedAssemblies.Add(c));

            compilerParams.ReferencedAssemblies.Add(Assembly.GetCallingAssembly().Location);

            return compilerParams;
        }
        private RuntimeCompilationException GetRuntimeAssemblyCompileException(string classCode, CompilerResults compilerResults, CompilerParameters compilerParams)
        {
            List<CompileErrorInfo> compileErrorInfoList = new List<CompileErrorInfo>();

            foreach (CompilerError error in compilerResults.Errors)
            {
                CompileErrorInfo errorInfo = new CompileErrorInfo(error);
                compileErrorInfoList.Add(errorInfo);
            }

            StringBuilder messageBuilder = new StringBuilder();

            messageBuilder.AppendLine();
            messageBuilder.AppendLine("Code:");
            messageBuilder.AppendLine(classCode);
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("Errors:");
            compileErrorInfoList.ForEach(c => messageBuilder.AppendLine(c.ToString()));

            return new RuntimeCompilationException(
                messageBuilder.ToString(),
                compileErrorInfoList,
                compilerParams.ReferencedAssemblies.OfType<string>());
        }
    }
}
