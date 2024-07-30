using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpAlla
{
    public enum TokenType
    {
        Or,
        And,
        Equal,
        Unequal,
        Plus,
        Minus,
        Multiply,
        Divide,
        Number,
        Identifier,
        LeftParen,
        RightParen,
        String,
        Bool,
        Assign,
        Negation,
        Function,
        Return,
        LeftBrace,
        RightBrace,
        Comma,
        Statement,
        Class,
        Dot,
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return Type + ", " + Value;
        }
    }

    public class Lexer
    {
        private string _text;
        private int _pos = -1;
        private char _currentChar;

        public Lexer(string text)
        {
            _text = text + '\n';
            Advance();
        }

        private void Advance()
        {
            _pos++;
            _currentChar = _pos < _text.Length ? _text[_pos] : '\0';
        }

        private void Eat(char character)
        {
            if (_pos + 1 < _text.Length && _text[_pos + 1] == character)
                Advance();
            else
                throw new Exception($"Syntax error: Except \"{character}\"");
        }

        private void SkipWhitespace()
        {
            while (_currentChar != '\0' && char.IsWhiteSpace(_currentChar))
            {
                Advance();
            }
        }

        private string Number(List<Token> tokens)
        {
            string result = string.Empty;
            while (_currentChar != '\0' && char.IsDigit(_currentChar) || _currentChar == '.')
            {
                result += _currentChar;
                Advance();
            }
            return result;
        }

        private string String()
        {
            string result = "";
            Advance();
            while (_currentChar != '\0' && _currentChar != '"')
            {
                result += _currentChar;
                Advance();
            }
            Advance();
            return result;
        }

        private string Identifier()
        {
            string result = string.Empty;
            while (_currentChar != '\0' && char.IsLetterOrDigit(_currentChar))
            {
                result += _currentChar;
                Advance();
            }
            return result;
        }

        private void Comment()
        {
            while (_currentChar != '\n')
                Advance();
            Advance();
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            while (_currentChar != '\0')
            {
                if (char.IsWhiteSpace(_currentChar))
                {
                    SkipWhitespace();
                    continue;
                }

                if (char.IsDigit(_currentChar) || (_currentChar == '.' && char.IsDigit(_text[_pos + 1])))
                {
                    tokens.Add(new Token(TokenType.Number, Number(tokens)));
                    continue;
                }

                if (_currentChar == '"')
                {
                    tokens.Add(new Token(TokenType.String, String()));
                    continue;
                }

                if (char.IsLetter(_currentChar))
                {
                    string identifier = Identifier();
                    if (identifier == "true" || identifier == "false")
                    {
                        tokens.Add(new Token(TokenType.Bool, identifier));
                        continue;
                    }
                    switch(identifier)
                    {
                        case "true":
                        case "false":
                            tokens.Add(new Token(TokenType.Bool, identifier));
                            continue;
                        case "function":
                            tokens.Add(new Token(TokenType.Function, identifier));
                            continue;
                        case "return":
                            tokens.Add(new Token(TokenType.Return, identifier));
                            continue;
                        case "if":
                        case "else":
                            tokens.Add(new Token(TokenType.Statement, identifier));
                            continue;
                        case "class":
                            tokens.Add(new Token(TokenType.Class, identifier));
                            continue;
                    }
                    tokens.Add(new Token(TokenType.Identifier, identifier));
                    continue;
                }

                if (_currentChar == '#')
                {
                    Comment();
                    continue;
                }

                switch (_currentChar)
                {
                    case '+':
                        tokens.Add(new Token(TokenType.Plus, "+"));
                        break;
                    case '-':
                        tokens.Add(new Token(TokenType.Minus, "-"));
                        break;
                    case '*':
                        tokens.Add(new Token(TokenType.Multiply, "*"));
                        break;
                    case '/':
                        tokens.Add(new Token(TokenType.Divide, "/"));
                        break;
                    case '!':
                        Advance();
                        if (_currentChar == '=')
                        {
                            tokens.Add(new Token(TokenType.Unequal, "!="));
                            Advance();
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Negation, "!"));
                        }
                        continue;
                    case '=':
                        Advance();
                        if (_currentChar == '=')
                        {
                            tokens.Add(new Token(TokenType.Equal, "=="));
                            Advance();
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Assign, "="));
                        }
                        continue;
                    case '(':
                        tokens.Add(new Token(TokenType.LeftParen, "("));
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RightParen, ")"));
                        break;
                    case '&':
                        tokens.Add(new Token(TokenType.And, "&"));
                        break;
                    case '|':
                        tokens.Add(new Token(TokenType.Or, "|"));
                        break;
                    case '{':
                        tokens.Add(new Token(TokenType.LeftBrace, "{"));
                        break;
                    case '}':
                        tokens.Add(new Token(TokenType.RightBrace, "}"));
                        break;
                    case ',':
                        tokens.Add(new Token(TokenType.Comma, ","));
                        break;
                    case '.':
                        tokens.Add(new Token(TokenType.Dot, "."));
                        break;
                    default:
                        throw new Exception("Unknown character: " + _currentChar);
                }

                Advance();
            }

            //tokens.Add(new Token(TokenType.EndOfFile, string.Empty));
            return tokens;
        }
    }
}