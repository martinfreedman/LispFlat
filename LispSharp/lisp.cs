using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace LispFlat
{
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
                                          : throw new SyntaxErrorException($"Lookup {var} failed");
    }

    internal static class Lisp
    {
        // Read a Scheme expression from a string
        internal static Exp Parse(string program) => ReadFromTokens(Tokenize(program));

        // Convert a string to a queue of tokens
        static Queue<string> Tokenize(string s)
        {
            if (s == null)
                throw new SyntaxErrorException("no code");
            return new Queue<string>(s.Replace("(", " ( ").Replace(")", " ) ")
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // Read an expressions from a sequence of tokens
        static Exp ReadFromTokens(Queue<string> tokens)
        {
            if (!tokens.Any())
                throw new SyntaxErrorException("unexpected EOF while reading");

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
                throw new SyntaxErrorException("unexpected ')' ");
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
            ["list?"] = new Exp((a, e) => new Exp(a.IsList)),
            ["not"] = new Exp((a, e) => new Exp(a.IsBool ? !a.Bool : true)), 
            ["null?"] = new Exp((a, e) => new Exp(a.Head.Count==0)), 
            ["number?"] = new Exp((a, e) => new Exp(a.IsNum)),
            ["procedure?"] = new Exp((a, e) => new Exp(a.IsProc)),
            ["symbol?"] = new Exp((a, e) => new Exp(a.IsSym)),
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
            _globalEnv = _globalEnv ?? StandardEnv();
            env = env ?? _globalEnv ;

            if (x.IsSym) // variable reference - special forms below
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

    class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string message) : base(message) { }
    }
}