using System;
using System.Reflection;
using System.Reflection.Emit;
using static System.Activator;
using static System.Console;
using static System.Reflection.Assembly;
using static System.Threading.Thread;
using static System.Reflection.Emit.OpCodes;

namespace DynamicAsmBuilder
{
    class Program
    {
        static void Main()
        {
            WriteLine("***** The Amazing Dynamic Assembly Builder App *****");
            // Get the application domain for the current thread.
            AppDomain currentAppDomain = GetDomain();

            // Create the dynamic assembly using our helper f(x).
            CreateMyAsm(currentAppDomain);
            WriteLine("-> Finished creating MyAssembly.dll.");

            // Now load the new assembly from file.
            WriteLine("-> Loading MyAssembly.dll from file.");
            Assembly assembly = Load("MyAssembly");

            // Get the HelloWorld type.
            Type hello = assembly.GetType("MyAssembly.HelloWorld");

            // Create HelloWorld object and call the correct ctor.
            Write("-> Enter message to pass HelloWorld class: ");
            string message = ReadLine();
            object[] constructorArgs = new object[1];
            constructorArgs[0] = message;
            object obj = CreateInstance(hello, constructorArgs);

            // Call SayHello and show returned string.
            WriteLine("-> Calling SayHello() via late binding.");
            MethodInfo methodInfo = hello.GetMethod("SayHello");
            methodInfo.Invoke(obj, null);

            // Invoke method.
            methodInfo = hello.GetMethod("GetMsg");
            WriteLine(methodInfo.Invoke(obj, null));

        }

        // The caller sends in an AppDomain type.
        public static void CreateMyAsm(AppDomain currentAppDomain)
        {
            // Establish general assembly characteristics.
            AssemblyName assemblyName = new AssemblyName
            {
                Name = "MyAssembly",
                Version = new Version("1.0.0.0")
            };

            // Create new assembly within the current AppDomain.
            AssemblyBuilder assembly =
              currentAppDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);

            // Given that we are building a single-file
            // assembly, the name of the module is the same as the assembly.
            ModuleBuilder module =
              assembly.DefineDynamicModule("MyAssembly", "MyAssembly.dll");

            // Define a public class named "HelloWorld".
            TypeBuilder helloWorldClass = module.DefineType("MyAssembly.HelloWorld",
              TypeAttributes.Public);

            // Define a private String member variable named "theMessage".
            FieldBuilder msgField =
              helloWorldClass.DefineField("theMessage", Type.GetType("System.String"),
              attributes: FieldAttributes.Private);

            // Create the custom ctor.
            Type[] constructorArgs = new Type[1];
            constructorArgs[0] = typeof(string);
            ConstructorBuilder constructor =
              helloWorldClass.DefineConstructor(MethodAttributes.Public,
              CallingConventions.Standard,
              constructorArgs);
            ILGenerator constructorIL = constructor.GetILGenerator();
            constructorIL.Emit(Ldarg_0);
            Type objectClass = typeof(object);
            ConstructorInfo superConstructor =
              objectClass.GetConstructor(new Type[0]);
            constructorIL.Emit(Call, superConstructor);
            constructorIL.Emit(Ldarg_0);
            constructorIL.Emit(Ldarg_1);
            constructorIL.Emit(Stfld, msgField);
            constructorIL.Emit(Ret);

            // Create the default ctor.
            helloWorldClass.DefineDefaultConstructor(MethodAttributes.Public);
            // Now create the GetMsg() method.
            MethodBuilder getMsgMethod =
              helloWorldClass.DefineMethod("GetMsg", MethodAttributes.Public,
              typeof(string), null);
            ILGenerator methodIL = getMsgMethod.GetILGenerator();
            methodIL.Emit(Ldarg_0);
            methodIL.Emit(Ldfld, msgField);
            methodIL.Emit(Ret);

            // Create the SayHello method.
            MethodBuilder sayHiMethod =
              helloWorldClass.DefineMethod("SayHello",
              MethodAttributes.Public, null, null);
            methodIL = sayHiMethod.GetILGenerator();
            methodIL.EmitWriteLine("Hello from the HelloWorld class!");
            methodIL.Emit(Ret);

            // "Bake" the class HelloWorld.
            // (Baking is the formal term for emitting the type.)
            helloWorldClass.CreateType();

            // (Optionally) save the assembly to file.
            assembly.Save("MyAssembly.dll");
        }
    }
}