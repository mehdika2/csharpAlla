using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpAlla
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string code = @"
#class Person {
#  function Person(name, family) {
#    this.name = name
#    this.family = family
#  }

#  function GetFullname() {
#    return name + "" "" + family
#  }
#}

#ali = Person(""Ali"", ""Rezaee"")
#writeline(ali.name)

name = ""mahdi""
family = ""khalilzadeh""
fullname = name + "" "" + family
writeline(fullname, fullname == ""mahdi khalilzadeh"")

function sum(a,b) {
  return a + b
}

function test(t) {
  result = sum(t, t * 2)
  writeline(result)
  return result
}

writeline(test(10))
";

            long totalMiliseconds = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Lexer lexer = new Lexer(code);
            List<Token> tokens = lexer.Tokenize();

            Console.WriteLine("[Lexer Finish] " + sw.ElapsedMilliseconds + "ms");
            totalMiliseconds += sw.ElapsedMilliseconds;
            sw.Reset();
            sw.Start();

            Parser parser = new Parser(tokens);
            parser.Parse();

            Console.WriteLine("[Parser Finish] " + sw.ElapsedMilliseconds + "ms");
            totalMiliseconds += sw.ElapsedMilliseconds;
            sw.Reset();
            sw.Start();

            try
            {
                Interpreter interpreter = new Interpreter();
                //Console.WriteLine(interpreter.Disassemble(parser));
                interpreter.Interpret(parser);
                //Console.WriteLine(interpreter.variables.Reverse<object>().FirstOrDefault());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("[Interpreter Finish] " + sw.ElapsedMilliseconds + "ms");
            totalMiliseconds += sw.ElapsedMilliseconds;
            sw.Reset();

            Console.WriteLine("[Total] " + totalMiliseconds + "ms");

            Console.WriteLine(parser.constants.Count + " Constrator");
            Console.WriteLine(parser.variables.Count + " Variables");
            Console.WriteLine(parser.bytecodes.Count + " Bytecodes");

            //Console.WriteLine("========= Disassembled =========");

            //Console.WriteLine(interpreter.Disassemble(parser));
        }
    }
}
