﻿#region License and Terms
// LispFlat - A toy Lisp interpretor in C#
// Copyright (c) 2018 Martin Freedman. All rights reserved.
// 
// Derived from python code  (c) Peter Norvig, 2010-16 (See http://norvig.com/lispy.html)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;

namespace LispFlat
{
    internal enum Type { Num, Bool, Sym, List, Proc }

    internal class Exp
    {
        public string Sym { get; }
        public double Num { get; }
        public bool Bool { get; }
        public List<Exp> List { get; }
        public Func<Exp, Env, Exp> Inv { get; }
        public Type Type;

        // Atoms
        public Exp(string sym) { Sym = sym; Type = Type.Sym; }
        public Exp(double dbl) { Num = dbl; Type = Type.Num; }
        public Exp(bool @bool) { Bool = @bool; Type = Type.Bool; }
        // lists
        public Exp(List<Exp> exp) { List = exp; Type = Type.List; }
        public Exp(IEnumerable<Exp> exp) { List = exp.ToList(); Type = Type.List; }
        // lambdas/procedures
        public Exp(Func<Exp, Env, Exp> inv) { Inv = inv; Type = Type.Proc; }

        public int Count => List?.Count ?? -1;
        public Exp Head => List[0];
        public Exp Rest => new Exp(List.Skip(1));
        public Exp this[int i] => List[i];

        public override string ToString()
        {
            if (Type == Type.Bool)
                return Bool == true ? "#t" : "#f";
            if (Type == Type.Num)
                return Num.ToString();
            if (Type == Type.Sym)
                return Sym;
            if (Type == Type.Proc)
                return "<function>"; ;
            if (Type == Type.List)
                return Count > 0 ? $"({string.Join(" ", List.Select(i => i.ToString()))})" : "";
            return "stringify error";
        }
    }

    //"An environment: a dict of {'var':val} pairs, with an outer Env."
    internal class Env : Dictionary<string, Exp>
    {
        Env _outer;

        public Env(Exp parms = null, Exp args = null, Env outer = null)
        {
            if (parms != null)           
                foreach (var (parm, arg) in parms.List.Zip(args.List, (p, a) => (p.Sym, a)))
                    Add(parm, arg);
            _outer = outer;
        }

        //"Find the innermost Env where var appears."
        public Env Find(string var) => 
            TryGetValue(var, out Exp val) ? this: 
            _outer != null                ? _outer.Find(var) 
                                          : throw new LispException($"Lookup {var} failed");
    }

    internal static class Lisp
    {
        // Read a Scheme expression from a string
        internal static Exp Parse(string program) => ReadFromTokens(Tokenize(program));

        // Convert a string to a queue of tokens
        static Queue<string> Tokenize(string s)
        {
            if (s == null)
                throw new LispException("no code");
            return new Queue<string>(s.Replace("(", " ( ").Replace(")", " ) ")
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // Read an expressions from a sequence of tokens
        static Exp ReadFromTokens(Queue<string> tokens)
        {
            if (!tokens.Any())
                throw new LispException("unexpected EOF while reading");

            var token = tokens.Dequeue();
            if ("(" == token)
            {
                var L = new List<Exp>();
                while (tokens.Peek() != ")")
                    L.Add(ReadFromTokens(tokens));
                tokens.Dequeue(); // ")"
                return new Exp(L);
            }
            else if (")" == token)
                throw new LispException("unexpected ')' ");
            else
                return Atom(token);
        }

        // "Numbers become numbers; true/false becomes bools; every other token is a symbol."
        static Exp Atom(string token)
        {
            if (double.TryParse(token, out double resDbl))
                return new Exp(resDbl);
            if (token == "#t")
                return new Exp(true);
            if (token == "#f")
                return new Exp(false);
            if (token == "")
                return new Exp(new List<Exp>(0));
            return new Exp(token);
        }

        //"An environment with some Scheme standard procedures."
        internal static Env StandardEnv() => new Env
        {
            ["*"] = new Exp((a, e) => new Exp(a[0].Num * a[1].Num)),
            ["+"] = new Exp((a, e) => new Exp(a[0].Num + a[1].Num)),
            ["-"] = new Exp((a, e) => new Exp(a[0].Num - a[1].Num)),
            ["/"] = new Exp((a, e) => new Exp(a[0].Num / a[1].Num)),
            ["%"] = new Exp((a, e) => new Exp(a[0].Num % a[1].Num)),
            ["<"] = new Exp((a, e) => new Exp(a[0].Num < a[1].Num)),
            ["="] = new Exp((a, e) => new Exp(a[0].Num == a[1].Num)),
            [">"] = new Exp((a, e) => new Exp(a[0].Num > a[1].Num)),
            [">="] = new Exp((a, e) =>new Exp(a[0].Num >= a[1].Num)),
            ["<="] = new Exp((a, e) =>new Exp(a[0].Num <= a[1].Num)),
            ["append"] = new Exp((a,e)=> new Exp(a.Head.List.Concat(a.Rest.Head.List))),
            ["car"] = new Exp((a, e) => a.Head.Head),
            ["cdr"] = new Exp((a, e) => a.Head.Rest),
            ["cons"] = new Exp((a, e) => new Exp(a.Rest.Head.List.Prepend(a.Head))),
            ["length"] = new Exp((a, e) => new Exp(a.Head.Count)),
            ["list"] = new Exp((a, e) => a),
            ["list?"] = new Exp((a, e) => new Exp(a.Type == Type.List)),
            ["not"] = new Exp((a, e) => new Exp(a.Type== Type.Bool ? !a.Bool : false)), 
            ["null?"] = new Exp((a, e) => new Exp(a.Head.Count==0)), 
            ["number?"] = new Exp((a, e) => new Exp(a.Type == Type.Num)),
            ["procedure?"] = new Exp((a, e) => new Exp(a.Type == Type.Proc)),
            ["symbol?"] = new Exp((a, e) => new Exp(a.Type == Type.Sym)),
        };

        //.................. Interaction: A REPL .......................
        static Env _globalEnv;

        //"A prompt-read-eval-print loop."
        public static void Repl(string prompt = "list.cs", Env env =null)
        {
            _globalEnv = env ?? StandardEnv();

            Title = prompt;
            while (true)
            {
                Write(prompt + ">");
                var val = Eval(Parse(ReadLine()), _globalEnv);
                var str = val.ToString();
                if (!string.IsNullOrEmpty(str))
                    WriteLine(str);
            }
        }

        //"Evaluate an expression in an environment."
        internal static Exp Eval(Exp x, Env env=null)
        {
            env = env ?? _globalEnv ;

            if (x.Type == Type.Sym) // variable reference - special forms below
                return env.Find(x.Sym)[x.Sym];
            else if (x.Count <1) // constant literal - Num or Bool
                return x;
            else if (x[0].Sym == "quote") // (qoute exp)
                return x.Rest.Head;
            else if (x[0].Sym == "if") // (if test conseq alt)
            {
                var (test, conseq, alt) = (x[1], x[2], x[3]);
                var exp = (bool)Eval(test, env).Bool ? conseq : alt;
                return Eval(exp, env);
            }
            else if (x[0].Sym == "define") // (define var exp)
            {
                var (var, exp) = (x[1], x[2]);
                env[var.Sym] = Eval(exp, env);
                return new Exp(""); //nop
            }
            else if (x[0].Sym == "set!")  // (set! var exp)
            {
                var (var, exp) = (x[1], x[2]);
                env.Find(var.Sym)[var.Sym] = Eval(exp, env);
                return new Exp(""); //nop
            }
            else if (x[0].Sym == "lambda") // (lambda (var...) body )
            {
                var (parms, body) = (x[1], x[2]);
                return Procedure(parms, body, env);
            }
            else if (x[0].Sym == "begin") // (begin exp+)
            {
                Exp last = null;
                foreach (var exp in x.Rest.List)
                    last = Eval(exp, env);
                return last;
            }
            else                                // proc arg
            {
                var proc = Eval(x.Head, env);
                var args = from exp in x.Rest.List select Eval(exp, env);
                return proc.Inv(new Exp(args), env);
            }
        }

        //"A user-defined Scheme procedure."
        static Exp Procedure(Exp parms, Exp body, Env env) => new Exp((Exp args, Env e) => Eval(body, new Env(parms, args, env)));
    }

    class LispException : Exception
    {
        public LispException(string message) : base(message) { }
    }
}