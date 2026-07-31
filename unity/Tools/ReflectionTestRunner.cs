using System;
using System.Reflection;
using NUnit.Framework;

internal static class ReflectionTestRunner
{
    private static int Main(string[] args)
    {
        var failed = 0;
        var passed = 0;

        foreach (var assemblyPath in args)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            foreach (var type in assembly.GetTypes())
            {
                var instance = type.IsAbstract ? null : Activator.CreateInstance(type);
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    var testCases = method.GetCustomAttributes(typeof(TestCaseAttribute), false);
                    if (testCases.Length > 0)
                    {
                        foreach (TestCaseAttribute testCase in testCases)
                        {
                            Run(instance, method, testCase.Arguments, ref passed, ref failed);
                        }

                        continue;
                    }

                    if (method.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
                    {
                        Run(instance, method, Array.Empty<object>(), ref passed, ref failed);
                    }
                }
            }
        }

        Console.WriteLine("RESULT passed={0} failed={1}", passed, failed);
        return failed == 0 ? 0 : 1;
    }

    private static void Run(
        object instance,
        MethodInfo method,
        object[] arguments,
        ref int passed,
        ref int failed)
    {
        var name = method.DeclaringType.FullName + "." + method.Name;
        try
        {
            method.Invoke(instance, arguments);
            passed++;
            Console.WriteLine("PASS " + name);
        }
        catch (TargetInvocationException exception)
        {
            failed++;
            Console.WriteLine("FAIL " + name + ": " + exception.InnerException);
        }
    }
}
