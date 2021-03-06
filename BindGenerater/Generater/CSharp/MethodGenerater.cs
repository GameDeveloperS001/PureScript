using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generater
{
    public class MethodGenerater : CodeGenerater
    {
        MethodDefinition genMethod;
        CodeWriter writer;
        public MethodGenerater(MethodDefinition method)
        {
            genMethod = method;

            if (!genMethod.IsPublic && !genMethod.DeclaringType.IsInterface)
                return;
            if (genMethod.IsConstructor && genMethod.DeclaringType.IsAbstract)
                return;

            foreach (var p in genMethod.Parameters)
            {
                var type = p.ParameterType;
                Binder.AddType(type.Resolve());
            }
            Binder.AddType(genMethod.ReturnType.Resolve());

            if (!method.IsAbstract)
                GenerateBindings.AddMethod(genMethod);
        }

        public override void Gen()
        {
            writer = CS.Writer;
            base.Gen();

            if (!genMethod.IsPublic && !genMethod.DeclaringType.IsInterface)
                return;
            if (genMethod.IsConstructor && genMethod.DeclaringType.IsAbstract)
                return;

            if (genMethod.IsGetter)
                GenGeter();
            else if (genMethod.IsSetter)
                GenSeter();
            else if (genMethod.IsAddOn)
                GenAddOn();
            else if (genMethod.IsRemoveOn)
                GenRemoveOn();
            else if (genMethod.IsAbstract)
                GenAbstract();
            else
                GenMethod();
        }

        void GenGeter()
        {
            if(genMethod.IsAbstract)
            {
                writer.WriteLine("get");
                return;
            }
            writer.Start("get");
            var res =  MethodResolver.Resolve(genMethod).Call("res");
            writer.WriteLine("ScriptEngine.CheckException()");
            writer.WriteLine($"return {res}");
            writer.End();
        }

        void GenSeter()
        {
            if (genMethod.IsAbstract)
            {
                writer.WriteLine("set");
                return;
            }

            writer.Start("set");
            MethodResolver.Resolve(genMethod).Call("");
            writer.WriteLine("ScriptEngine.CheckException()");
            writer.End();
        }

        void GenAddOn()
        {
            writer.Start("add");
            MethodResolver.Resolve(genMethod).Call("");
            writer.WriteLine("ScriptEngine.CheckException()");
            writer.End();
        }

        void GenRemoveOn()
        {
            writer.Start("remove");
            MethodResolver.Resolve(genMethod).Call("");
            writer.WriteLine("ScriptEngine.CheckException()");
            writer.End();
        }

        void GenMethod()
        {
            writer.Start(GetMethodDelcear());
            if(genMethod.IsConstructor)
            {
                if(genMethod.DeclaringType.IsValueType)
                {
                    CS.Writer.WriteLine(Utils.BindMethodName(genMethod));
                    writer.WriteLine("ScriptEngine.CheckException()");
                }
                else
                {
                    CS.Writer.WriteLine($"var h = {Utils.BindMethodName(genMethod)}");
                    writer.WriteLine("ScriptEngine.CheckException()");
                    CS.Writer.WriteLine($"SetHandle(h)");
                    CS.Writer.WriteLine("ObjectStore.Store(this, h)");
                }
            }
            else
            {
                var res = MethodResolver.Resolve(genMethod).Call("res");
                writer.WriteLine("ScriptEngine.CheckException()");
                writer.WriteLine($"return {res}");
            }
            
            writer.End();
        }

        void GenAbstract()
        {
            writer.WriteLine(GetMethodDelcear());
        }


        string GetMethodDelcear()
        {

            string declear = "public ";
            if (genMethod.IsStatic)
                declear += "static ";

            if (Utils.IsUnsafeMethod(genMethod))
                declear += "unsafe ";

            if (genMethod.IsAbstract)
                declear += "abstract ";
            else if (genMethod.IsOverride())
                declear += "override ";
            else if (genMethod.IsVirtual && !genMethod.IsFinal)
                declear += "virtual ";

            var methodName = genMethod.Name;

            switch(methodName)
            {
                case "op_Addition":
                    methodName = "operator+";
                    break;
                case "op_Subtraction":
                    methodName = "operator-";
                    break;
                case "op_UnaryNegation":
                    methodName = "operator-";
                    break;
                case "op_Multiply":
                    methodName = "operator*";
                    break;
                case "op_Division":
                    methodName = "operator/";
                    break;
                case "op_Equality":
                    methodName = "operator==";
                    break;
                case "op_Inequality":
                    methodName = "operator!=";
                    break;
                case "op_Implicit":
                    methodName = "implicit operator " + genMethod.ReturnType.Name;
                    break;
                case "op_Explicit":
                    methodName = "explicit operator " + genMethod.ReturnType.Name;
                    break;

            }

            if (!genMethod.IsConstructor)
            {
                if(!methodName.StartsWith("implicit")&& !methodName.StartsWith("explicit"))
                    declear += TypeResolver.Resolve(genMethod.ReturnType).RealTypeName() + " ";
                declear += methodName;
            }
            else
            {
                declear += genMethod.DeclaringType.Name;
            }

            var param = "(";
            var lastP = genMethod.Parameters.LastOrDefault();
            foreach (var p in genMethod.Parameters)
            {
                var type = p.ParameterType;
                var typeName = TypeResolver.Resolve(type).RealTypeName();
                if (type.IsGenericInstance)
                    typeName = Utils.GetGenericTypeName(type);

                param += $"{typeName} {p.Name}" + (p == lastP ? "" : ", ");
            }
            param += ")";

            declear += param;

            if (genMethod.IsConstructor && genMethod.DeclaringType.IsValueType)
                declear += ":this()";

            return declear;
        }
    }
}