using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpAlla
{
    internal class Interpreter
    {
        Stack<object> stack;
        public List<object> variables;
        object[] constants;
        private Dictionary<byte, Action<byte>> bytecodeHandlers;
        Parser parser;
        int position;

        /// <summary>
        /// execute bytes codes
        /// </summary>
        /// <returns>last item in the code stack</returns>
        public object Interpret(Parser parser)
        {
            stack = new Stack<object>();
            if(variables == null)
                variables = new List<object>();

            constants = parser.constants.ToArray();

            this.parser = parser;

            bytecodeHandlers = new Dictionary<byte, Action<byte>>
            {
                { 0x01, Load_Const },
                { 0x02, Load_Var },
                { 0x03, Store_Var },
                { 0x04, Compare_OP },
                { 0x05, Binary_OP },
                { 0x06, Unary_Negative },
                { 0x07, Jump_If_True_Or_Pop },
                { 0x08, Jump_If_False_Or_Pop },
                { 0x09, Pop_Jump_Forward_If_False },
                { 0x0a, Pop_Jump_Forward_If_True },
                { 0x0b, Start },
                { 0x0c, Call },
                { 0x0d, Return },
                { 0x0e, Jump_Forward },
                { 0x0f, Load_Prop },
            };

            for (position = 0; position < parser.bytecodes.Count; position++)
            {
                Action<byte> handler;
                if (bytecodeHandlers.TryGetValue(parser.bytecodes[position], out handler))
                    handler(parser.bytecodes[++position]);
                else
                    throw new InvalidOperationException($"Unknown bytecode: {parser.bytecodes[position]}");
            }

            if (stack.Any())
                return stack.Pop();
            return null;
        }

        public string Disassemble(Parser parser)
        {
            stack = new Stack<object>();
            variables = new List<object>();

            constants = parser.constants.ToArray();

            this.parser = parser;

            bytecodeHandlers = new Dictionary<byte, Action<byte>>
            {
                { 0x01, Load_Const },
                { 0x02, Load_Var },
                { 0x03, Store_Var },
                { 0x04, Compare_OP },
                { 0x05, Binary_OP },
                { 0x06, Unary_Negative },
                { 0x07, Jump_If_True_Or_Pop },
                { 0x08, Jump_If_False_Or_Pop },
                { 0x09, Pop_Jump_Forward_If_False },
                { 0x0a, Pop_Jump_Forward_If_True },
                { 0x0b, Start },
                { 0x0c, Call },
                { 0x0d, Return },
                { 0x0e, Jump_Forward },
                { 0x0f, Load_Prop },
            };

            StringBuilder sb = new StringBuilder();
            for (position = 0; position < parser.bytecodes.Count; position++)
            {
                byte ___b = parser.bytecodes[position];
                byte ___n = parser.bytecodes[position + 1];
                Action<byte> handler;
                if (bytecodeHandlers.TryGetValue(parser.bytecodes[position], out handler))
                {
                    int lastPosition = position;
                    handler(parser.bytecodes[++position]);
                    position = ++lastPosition;
                    sb.AppendLine($"{parser.bytecodes[position - 1]}\t({handler.Method.Name})\t{___n} ({GetBytecodeValue(___b, ___n, parser)})");
                }
                else
                    throw new InvalidOperationException($"Unknown bytecode: {parser.bytecodes[position]}");
            }

            return sb.ToString();
        }

        string GetBytecodeValue(byte action, byte value, Parser parser)
        {
            switch (action)
            {
                case 0x01:
                    return constants[value].ToString();
                case 0x02:
                case 0x03:
                    return parser.variables[value].ToString();
                case 0x04:
                    if (value == 0x10)
                        return "==";
                    else
                        return "!=";
                case 0x05:
                    if (value == 0x20)
                        return "+";
                    else if (value == 0x21)
                        return "-";
                    else if (value == 0x22)
                        return "*";
                    else
                        return "/";
                case 0x07:
                case 0x08:
                    return value.ToString();
                default:
                    return "";
            }
        }

        void Load_Const(byte code)
        {
            stack.Push(constants[code]);
        }

        void Load_Var(byte code)
        {
            if (code >= variables.Count)
                throw new Exception($"'{parser.variables[code]}' is not defined");
            stack.Push(variables[code]);
        }

        void Store_Var(byte code)
        {
            if (code >= variables.Count)
            {
                variables.Add(stack.Pop());
                return;
            }
            variables[code] = stack.Pop();
        }

        void Compare_OP(byte code)
        {
            object e1 = stack.Pop();
            object e2 = stack.Pop();

            if (IsNumberType(e1) && IsNumberType(e2))
            {
                double d1 = Convert.ToDouble(e1);
                double d2 = Convert.ToDouble(e2);

                if (code == 0x10)
                {
                    stack.Push(d1 == d2);
                }
                else
                {
                    stack.Push(d1 != d2);
                }
            }
            else
            {
                if (code == 0x10)
                {
                    stack.Push(e1.Equals(e2));
                }
                else
                {
                    stack.Push(!e1.Equals(e2));
                }
            }
        }

        void Binary_OP(byte code)
        {
            object e2 = stack.Pop();
            object e1 = stack.Pop();
            if (IsNumberType(e1) && IsNumberType(e2))
            {
                double num1 = Convert.ToDouble(e1);
                double num2 = Convert.ToDouble(e2);
                switch (code)
                {
                    case 0x20:
                        stack.Push(num1 + num2);
                        break;
                    case 0x21:
                        stack.Push(num1 - num2);
                        break;
                    case 0x22:
                        stack.Push(num1 * num2);
                        break;
                    default:
                        stack.Push(num1 / num2);
                        break;
                }
            }
            else if (e1 is string || e2 is string)
                stack.Push(e1.ToString() + e2);
            else
                throw new Exception($"Cant do binary operation on '{e1.GetType().Name}' and '{e2.GetType().Name}'");
        }

        void Unary_Negative(byte code)
        {
            object num = stack.Pop();
            if (num is int)
                stack.Push(-(int)num);
            else if (num is float)
                stack.Push(-(float)num);
            else stack.Push(-(double)num);
        }

        void Jump_If_True_Or_Pop(byte code)
        {
            if ((bool)stack.Peek())
            {
                position += code;
                return;
            }
            stack.Pop();
        }

        void Jump_If_False_Or_Pop(byte code)
        {
            if (!(bool)stack.Peek())
            {
                position += code;
                return;
            }
            stack.Pop();
        }

        void Pop_Jump_Forward_If_True(byte code)
        {
            if ((bool)stack.Pop())
                position += code;
        }

        void Pop_Jump_Forward_If_False(byte code)
        {
            if (!(bool)stack.Pop())
                position += code;
        }

        void Start(byte code)
        {
            // strat function load all constants to variables
            for (int i = 0; i < parser.arguments.Count; i++)
                variables.Add(parser.arguments[i]);
        }

        void Call(byte code)
        {
            object funcObj = stack.Pop();
            List<object> arguments = new List<object>();
            for (int i = 0; i < code; i++)
                arguments.Add(stack.Pop());
            arguments.Reverse();
            if (funcObj is BuiltInFunction)
            {
                BuiltInFunction builtinFunction = (BuiltInFunction)funcObj;
                if (builtinFunction.Function is Parser.WriteDelegate)
                    (builtinFunction.Function as Parser.WriteDelegate)(arguments.First().ToString());
                if (builtinFunction.Function is Parser.WriteLineDelegate)
                    (builtinFunction.Function as Parser.WriteLineDelegate)(arguments.Select(i => i.ToString()).ToArray());
                else if (builtinFunction.Function is Parser.ReadDelegate)
                    stack.Push((builtinFunction.Function as Parser.ReadDelegate)());
                else if (builtinFunction.Function is Parser.ReadlineDelegate)
                    stack.Push((builtinFunction.Function as Parser.ReadlineDelegate)());
            }
            else
            {
                Function function = (Function)funcObj;
                Interpreter interpreter = new Interpreter();
                function.Parser.arguments = arguments;
                function.Parser.arguments.AddRange(new List<object>(variables));
                stack.Push(interpreter.Interpret(function.Parser));
            }
        }

        void Return(byte code)
        {
            position = parser.bytecodes.Count;
            //int pos = position - 1;
            //if(position >= 0)
            //{
            //     if (parser.bytecodes[pos] == 0x01)
            //    {
            //        Load_Const(parser.bytecodes[pos + 1]);
            //    }
            //    else if (parser.bytecodes[pos] == 0x02)
            //    {
            //        Load_Var(parser.bytecodes[pos + 1]);
            //    }
            //}
        }

        void Jump_Forward(byte code)
        {
            position += code;
        }

        void Load_Prop(byte code)
        {
            var property = (string)stack.Pop();
            var @class = (Class)stack.Pop();
            stack.Push(@class.Properties.First(i => i.Name == property));
        }

        static bool IsNumberType(object obj)
        {
            var s = obj.GetType();
            return obj is int || obj is float || obj is double || obj is decimal;
        }
    }

    public class BuiltInFunction
    {
        public string Name { get; set; }
        public Delegate Function { get; set; }

        public BuiltInFunction(string name, Delegate function)
        {
            Name = name;
            Function = function;
        }
    }

    public class Function
    {
        public string Name { get; set; }
        public Parser Parser { get; set; }

        public Function(string name, Parser parser)
        {
            Name = name;
            Parser = parser;
        }
    }

    public class Class
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; }

        public Class(string name, List<Property> properties)
        {
            Name = name;
            Properties = properties;
        }
    }

    public class Property
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public Property(string name, object Value)
        {
            Name = name;
            Value = Value;
        }
    }
}
