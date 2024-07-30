using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpAlla
{// Bring ifcall into expression
    public class Parser
    {
        List<Token> tokens;
        public List<object> constants = new List<object>();
        public List<string> variables = new List<string>();
        public List<byte> bytecodes = new List<byte>();
        public List<object> arguments = new List<object>();
        int position = -1;
        Token currentToken;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
            Advance();
        }

        public void Parse()
        {
            while (!IsAtEnd())
            {
                ParseExpression();
            }
        }

        void ParseExpression()
        {
            if (tokens.Count == 10)
                ;
            if (Match(TokenType.Identifier))
            {
                if (Check(TokenType.Assign))
                {
                    VariableDeclaration();
                }
                else
                {
                    Slip();
                    Expression();
                }
            }
            else if (Match(TokenType.Function))
            {
                FunctionDeclaration();
            }
            else if (Check(TokenType.Statement))
            {
                StatementExpression();
            }
            else if (Check(TokenType.Return))
            {
                Return();
            }
            else if (Match(TokenType.Class))
            {
                ClassDeclaration();
            }
            else
            {
                Expression();
                //throw new Exception("Syntax error.");
            }
        }

        void Return()
        {
            Eat(TokenType.Return);

            Expression();

            bytecodes.Add(0x0d); // return
            bytecodes.Add(0x0);
        }

        void ClassDeclaration()
        {
            string className = currentToken.Value;
            Eat(TokenType.Identifier);
            Eat(TokenType.LeftBrace);

            List<Property> properties = new List<Property>();
            while (!Check(TokenType.RightBrace))
            {
                if (Match(TokenType.Function))
                {
                    var function = FunctionDeclaration(true);
                    properties.Add(new Property(function.Name, function));
                }
                else if(Check(TokenType.Dot))
                {
                    if(Previous().Value != "this")
                        throw new Exception("Syntax error.");
                    PropertyDeclaration();
                }
                else
                {
                    throw new Exception("Syntax error.");
                }
            }

            Eat(TokenType.RightBrace);

            Class codeClass = new Class(className, properties);

            bytecodes.Add(0x01); // load_const
            bytecodes.Add(GetConstantIndex(codeClass));
            bytecodes.Add(0x03); // store_name
            bytecodes.Add(GetVariableIndex(className));
        }

        Function FunctionDeclaration(bool ret = false)
        {
            string functionName = currentToken.Value;
            Eat(TokenType.Identifier);
            Eat(TokenType.LeftParen);

            List<string> parameters = new List<string>(); // variables for interpreter
            while (!Check(TokenType.RightParen))
            {
                parameters.Add(currentToken.Value);
                Eat(TokenType.Identifier);
                if (!Match(TokenType.Comma)) break;
            }

            Eat(TokenType.RightParen);
            Eat(TokenType.LeftBrace);

            int start = bytecodes.Count;
            List<Token> functionTokens = new List<Token>();
            int openContext = 0;
            while (true)
            {
                if (Check(TokenType.LeftBrace))
                    openContext++;
                else if (Check(TokenType.RightBrace))
                {
                    openContext--;
                    if (openContext < 0)
                        break;
                }
                functionTokens.Add(currentToken);
                Eat(currentToken.Type);
            }
            Eat(TokenType.RightBrace);

            Parser functionParser = new Parser(functionTokens);
            functionParser.variables = parameters;
            functionParser.bytecodes.Add(0x0b); // start
            functionParser.bytecodes.Add(0);
            functionParser.variables.AddRange(variables);
            functionParser.Parse();

            Function codeFunction = new Function(functionName, functionParser);

            if(ret)
            {
                return codeFunction;
            }

            bytecodes.Add(0x01); // load_const
            bytecodes.Add(GetConstantIndex(codeFunction));
            bytecodes.Add(0x03); // store_name
            bytecodes.Add(GetVariableIndex(functionName));
            return null;
        }

        void VariableDeclaration()
        {
            string varName = Previous().Value;
            Eat(TokenType.Assign);

            int varIndex = GetVariableIndex(varName);

            Expression();

            bytecodes.Add(0x03); // StoreVar
            bytecodes.Add((byte)varIndex);
        }

        void StatementExpression()
        {
            string statement = currentToken.Value;

            switch (statement)
            {
                case "if":
                    IfStatement();
                    break;
                default:
                    throw new Exception("Wrong statement.");
            }
        }

        void IfStatement()
        {
            Eat(TokenType.Statement);
            Eat(TokenType.LeftParen);

            while (!Match(TokenType.RightParen))
            {
                ParseExpression();
                //Expression(); // for expression after call function)
                while (Match(TokenType.Or))
                {
                    bytecodes.Add(0x07); // JumpIfTrueOrPop
                    int jumpPos = bytecodes.Count;
                    bytecodes.Add(0); // Placeholder for jump offset

                    AndExpression();

                    bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos - 1);
                }
                while (Match(TokenType.And))
                {
                    bytecodes.Add(0x08); // JumpIfFalseOrPop
                    int jumpPos = bytecodes.Count;
                    bytecodes.Add(0); // Placeholder for jump offset

                    EqualityExpression();

                    bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos - 1);
                }
            }

            Eat(TokenType.LeftBrace);
            CodeblockStatement(0x09); // pop_jump_if_false

            if (currentToken.Type == TokenType.Statement && currentToken.Value == "else")
            {
                Eat(TokenType.Statement);

                bytecodes.Add(0x0e);
                int jumpPos = bytecodes.Count;
                bytecodes.Add(0); // placeholder for jump offset

                ElseStatement();

                bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos - 1);
            }
        }

        void ElseStatement()
        {
            if (currentToken.Type == TokenType.Statement)
                IfStatement();

            if (Match(TokenType.LeftBrace))
                while (!Match(TokenType.RightBrace))
                    ParseExpression();
        }

        void CodeblockStatement(byte jumpType)
        {
            bytecodes.Add(jumpType);
            int jumpPos = bytecodes.Count;
            bytecodes.Add(0); // placeholder for jump offset

            while (!Match(TokenType.RightBrace))
                ParseExpression();

            bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos + (Check(TokenType.Statement) ? 1 : -1));
        }

        void PropertyDeclaration()
        {
            string @base = Previous().Value;
            Eat(TokenType.Dot);

            if (!Check(TokenType.Identifier))
                throw new Exception("Except property goet " + currentToken.Type);

            string property = currentToken.Value;
            Eat(TokenType.Identifier);

            if(@base == "this")
            {
                if (Check(TokenType.Assign))
                {
                    VariableDeclaration();
                }
                else
                {
                    Slip();
                    Expression();
                }
            }
            else
            {
                bytecodes.Add(0x02); // load_var
                bytecodes.Add(GetVariableIndex(@base));
                bytecodes.Add(0x0f); // load_prop
                bytecodes.Add(GetConstantIndex(property));

                int argsCount = 1;
                int openParen = 0;
                while (true)
                {
                    if (Check(TokenType.LeftParen))
                        openParen++;
                    else if (Check(TokenType.RightParen))
                    {
                        openParen--;
                        if (openParen < 0)
                        {
                            if (Previous().Type == TokenType.LeftParen)
                                argsCount = 0;
                            break;
                        }
                    }
                    else if (Match(TokenType.Identifier))
                    {
                        if (Check(TokenType.LeftParen))
                        {
                            Slip();
                            ParseExpression();
                        }
                        else
                        {
                            Slip();
                            Expression();
                        }
                    }
                    else
                    {
                        Expression();
                    }
                    if (Match(TokenType.Comma))
                        argsCount++;
                }
                Eat(TokenType.RightParen);

                bytecodes.Add(0x0c); // call
                bytecodes.Add((byte)argsCount);
            }
        }

        void Expression()
            => OrExpression();

        void OrExpression()
        {
            AndExpression();

            while (Match(TokenType.Or))
            {
                bytecodes.Add(0x07); // JumpIfTrueOrPop
                int jumpPos = bytecodes.Count;
                bytecodes.Add(0); // Placeholder for jump offset

                AndExpression();

                bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos - 1);
            }
        }

        void AndExpression()
        {
            EqualityExpression();

            while (Match(TokenType.And))
            {
                bytecodes.Add(0x08); // JumpIfFalseOrPop
                int jumpPos = bytecodes.Count;
                bytecodes.Add(0); // Placeholder for jump offset

                EqualityExpression();

                bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos - 1);
            }
        }

        void EqualityExpression()
        {
            AdditiveExpression();
            while (Match(TokenType.Equal, TokenType.Unequal))
            {
                var opCode = GetOperationCode(Previous().Type);
                AdditiveExpression();
                bytecodes.Add(0x04); // CompareOp
                bytecodes.Add(opCode);
            }
        }

        void AdditiveExpression()
        {
            MultiplicativeExpression();
            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var opCode = GetBinaryCode(Previous().Type);
                MultiplicativeExpression();
                bytecodes.Add(0x05); // BinaryOp
                bytecodes.Add(opCode);
            }
        }

        void MultiplicativeExpression()
        {
            UnaryExpression();
            while (Match(TokenType.Multiply, TokenType.Divide))
            {
                var opCode = GetBinaryCode(Previous().Type);
                UnaryExpression();
                bytecodes.Add(0x05); // BinaryOp
                bytecodes.Add(opCode);
            }
        }

        void UnaryExpression()
        {
            if (Match(TokenType.Minus))
            {
                PrimaryExpression();
                bytecodes.Add(0x06); // unary variable
                bytecodes.Add(0x00); // skip

                if (currentToken.Type != TokenType.Identifier)
                    Advance();
            }
            else
            {
                PrimaryExpression();
            }
        }

        void PrimaryExpression()
        {
            Token token = currentToken;
            switch (token.Type)
            {
                case TokenType.Number:
                    Eat(TokenType.Number);
                    bytecodes.Add(0x01); // Load_Const
                    if (token.Value.ToString().Contains('.'))
                        bytecodes.Add(GetConstantIndex(double.Parse(token.Value.ToString())));
                    else bytecodes.Add(GetConstantIndex(int.Parse(token.Value.ToString())));
                    break;

                case TokenType.Identifier:
                    Eat(TokenType.Identifier);
                    if (currentToken.Type == TokenType.LeftParen)
                    {
                        FunctionExpression();
                        break;
                    }
                    bytecodes.Add(0x02); // Load_Var
                    bytecodes.Add(GetVariableIndex(token.Value));
                    break;

                case TokenType.LeftParen:
                    Eat(TokenType.LeftParen);
                    Expression();
                    Eat(TokenType.RightParen);
                    break;

                case TokenType.Bool:
                    Eat(TokenType.Bool);
                    bytecodes.Add(0x01); // Load_Const
                    bytecodes.Add(GetConstantIndex(bool.Parse(token.Value)));
                    break;

                case TokenType.String:
                    Eat(TokenType.String);
                    bytecodes.Add(0x01); // Load_Const
                    bytecodes.Add(GetConstantIndex(token.Value));
                    break;

                default:
                    throw new Exception($"Unexpected token {token.Type}.");
            }
        }

        void FunctionExpression()
        {
            string functionName = Previous().Value;
            Eat(TokenType.LeftParen);

            int argsCount = 1;
            int openParen = 0;
            while (true)
            {
                if (Check(TokenType.LeftParen))
                    openParen++;
                else if (Check(TokenType.RightParen))
                {
                    openParen--;
                    if (openParen < 0)
                    {
                        if (Previous().Type == TokenType.LeftParen)
                            argsCount = 0;
                        break;
                    }
                }
                else if (Match(TokenType.Identifier))
                {
                    if (Check(TokenType.LeftParen))
                    {
                        Slip();
                        ParseExpression();
                    }
                    else
                    {
                        Slip();
                        Expression();
                    }
                }
                else
                {
                    Expression();
                }
                if (Match(TokenType.Comma))
                    argsCount++;
            }
            Eat(TokenType.RightParen);

            switch (functionName)
            {
                case "write":
                    bytecodes.Add(0x01); // load_const
                    bytecodes.Add(GetConstantIndex(new BuiltInFunction(functionName, new WriteDelegate(Write))));
                    break;
                case "writeline":
                    bytecodes.Add(0x01); // load_const
                    bytecodes.Add(GetConstantIndex(new BuiltInFunction(functionName, new WriteLineDelegate(WriteLine))));
                    break;
                case "read":
                    bytecodes.Add(0x01); // load_const
                    bytecodes.Add(GetConstantIndex(new BuiltInFunction(functionName, new ReadDelegate(Read))));
                    break;
                case "readline":
                    bytecodes.Add(0x01); // load_const
                    bytecodes.Add(GetConstantIndex(new BuiltInFunction(functionName, new ReadlineDelegate(Readline))));
                    break;
                default:
                    bytecodes.Add(0x02); // load_var
                    bytecodes.Add(GetVariableIndex(functionName));
                    break;
            }
            bytecodes.Add(0x0c); // call
            bytecodes.Add((byte)argsCount);
        }

        byte GetConstantIndex(object value)
        {
            int index = constants.IndexOf(value);
            if (index == -1)
            {
                constants.Add(value);
                index = constants.Count - 1;
            }
            return (byte)index;
        }

        byte GetVariableIndex(string varName)
        {
            int index = variables.IndexOf(varName);
            if (index == -1)
            {
                variables.Add(varName);
                index = variables.Count - 1;
            }
            return (byte)index;
        }

        bool Match(params TokenType[] types)
        {
            foreach (var type in types)
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            return false;
        }

        bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return currentToken.Type == type;
        }

        //bool Check(TokenType type, string value)
        //{
        //    if (IsAtEnd()) return false;
        //    return currentToken.Type == type && currentToken.Value == value; ;
        //}

        void Eat(TokenType type)
        {
            if (currentToken.Type == type)
                Advance();
            else
                throw new Exception($"Expected token {type}, got {currentToken.Type}");
        }

        void Advance()
        {
            position++;
            if (IsAtEnd()) return;
            currentToken = tokens[position];
        }

        void Slip()
        {
            if (position == 0) return;
            currentToken = tokens[--position];
        }

        /// <summary>
        /// Get previous token after match
        /// </summary>
        Token Previous()
        {
            return tokens[position - 1];
        }

        /// <summary>
        /// Get operation codes for 
        /// </summary>
        byte GetOperationCode(TokenType type)
        {
            switch (type)
            {
                case TokenType.Equal: return 0x10;
                case TokenType.Unequal: return 0x11;
                default: throw new Exception("Unknown operation");
            };
        }

        byte GetBinaryCode(TokenType type)
        {
            switch (type)
            {
                case TokenType.Plus: return 0x20;
                case TokenType.Minus: return 0x21;
                case TokenType.Multiply: return 0x22;
                case TokenType.Divide: return 0x23;
                default: throw new Exception("Unknown operation");
            };
        }

        bool IsAtEnd()
        {
            return position >= tokens.Count;
        }



        // built-in function
        public delegate void WriteDelegate(params string[] args);
        void Write(params string[] args)
        {
            foreach (string arg in args)
                Console.Write(arg);
        }

        public delegate void WriteLineDelegate(params string[] args);
        void WriteLine(params string[] args)
        {
            foreach (string arg in args)
                Console.WriteLine(arg);
        }

        public delegate char ReadDelegate();
        char Read()
        {
            return Console.ReadKey().KeyChar;
        }

        public delegate string ReadlineDelegate();
        string Readline()
        {
            return Console.ReadLine();
        }


        ////// Debug
        Token _Previous
        {
            get
            {
                return tokens[position - 1];
            }
        }
    }
}
