using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace LispSharp
{

    //"An environment: a dict of {'var':val} pairs, with an outer Env."
    internal class Env : Dictionary<string, Func<Exp, Env, Exp>>
    {
        Env _outer;

        public Env(Exp parms = null, Func<Exp, Env, Exp>[] args = null, Env outer = null)
        {
            if (parms != null)
                foreach (var (parm, arg) in parms.Expr.Zip(args, (p, a) => (p.Sym, a)))
                    Add(parm, arg);
            _outer = outer;
        }

        //"Find the innermost Env where var appears."
        public Env Find(string var) => !TryGetValue(var, out Func<Exp, Env, Exp> val) ? _outer?.Find(var) : this;
    }

    internal static class Lisp
    {
        // Read a Scheme expression from a string
        static Exp Parse(string program) => ReadFromTokens(Tokenize(program));

        // Convert a string to a list of tokens
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
            if (int.TryParse(token, out int resInt))
                return new Exp(resInt);
            if (double.TryParse(token, out double resDbl))
                return new Exp(resDbl);
            if (token == "#t")
                return new Exp(true);
            if (token == "#f")
                return new Exp(false);
            return new Exp(token);
        }

        //.................. Environments ........................

        //"An environment with some Scheme standard procedures."
        static Env StandardEnv()
        {
            var env = new Env
            {
                ["*"] = (a, e) => Math(a, e, "*"),
                ["+"] = (a, e) => Math(a, e, "+"),
                ["-"] = (a, e) => Math(a, e, "-"),
                ["/"] = (a, e) => Math(a, e, "/"),
                ["%"] = (a, e) => Math(a, e, "%"),
                ["<"] = (a, e) => Math(a, e, "<"),
                ["="] = (a, e) => Math(a, e, "="),
                [">"] = (a, e) => Math(a, e, ">"),
                [">="] = (a, e) => Math(a, e, ">="),
                ["<="] = (a, e) => Math(a, e, "<="),
                ["append"] = (a, e) => new Exp((a.Head.Concat(a.Rest.Expr)).ToList()), // TODO
                ["apply"] = (a, e) => new Exp(a.Rest.Expr.Select(x => a.Head.Inv(x, e)).ToList()), // TODO not map
                ["car"] = (a, e) => a.Head,
                ["cdr"] = (a, e) => a.Rest,
                ["cons"] = (a, e) => new Exp((new List<Exp> { a.Head }.Concat(a.Rest)).ToList()), //TODO
                ["eq?"] = (a, e) => Math(a, e, "="),
                ["equal?"] = (a, e) => Equal(a, e),
                ["length"] = (a, e) => new Exp(a.Count),
                ["list"] = (a, e) => new Exp(a.Expr),
                ["list?"] = (a, e) => new Exp(a.IsList),
                ["map"] = (a, e) => new Exp(a.Rest.Expr.Select(x => a.Head.Inv(x, e)).ToList()), //TODO
                ["not"] = (a, e) => new Exp(a.IsBool ? !a.Bool.HasValue : true), //TODO
                ["null?"] = (a, e) => new Exp(!a.IsList), //TODO
                ["number?"] = (a, e) => new Exp(a.IsNum),
                ["procedure?"] = (a, e) => new Exp(a.IsProc),
                ["symbol?"] = (a, e) => new Exp(a.IsSym),
            };

            return env;
        }

        //................... Primitives ..............................
        static Exp Math(Exp args, Env env, string op)
        {
            if (args.Count == 0)
                return new Exp("invalid args for math");
            if (args.Count != 2)
                return new Exp("incorrect amount of args for math");
            var xx = args[0];
            var yy = args[1];
            if (!xx.IsNum || !yy.IsNum)
                return new Exp("arguments are not numbers");
            dynamic x = xx.IsDbl ? xx.Dbl : xx.Int;
            dynamic y = yy.IsDbl ? yy.Dbl : yy.Int;

            switch (op)
            {
                case "+": return new Exp(x + y);
                case "-": return new Exp(x - y);
                case "/": return new Exp(x / y);
                case "*": return new Exp(x * y);
                case "%": return new Exp(x % y);
                case "=": return new Exp(x == y);
                case "<": return new Exp(x < y);
                case "<=": return new Exp(x <= y);
                case ">": return new Exp(x > y);
                case ">=": return new Exp(x >= y);
                default:
                    return new Exp("invalid math symbol");
            }
        }

        static Exp Equal(Exp args, Env env)
        {
            if (args.Count < 2)
                return new Exp("error");
            dynamic x = args.Expr[0];
            dynamic y = args.Expr[1];
            if (x.Expr.SequenceEqual(y.Expr))
                return new Exp(true);
            else
                return new Exp(false);
        }

        //.................. Interaction: A REPL .......................
        static Env _globalEnv;

        //"A prompt-read-eval-print loop."
        public static void Repl(string prompt = "list.cs")
        {
            _globalEnv = StandardEnv();

            Title = prompt;
            while (true)
            {
                WriteLine();
                Write(prompt + ">");
                var val = Eval(Parse(ReadLine()), _globalEnv);
                if (val != null)
                    Write(LispStr(val));
            }
        }

        //"Convert C# Exp object back into a Lisp-readable string."
        static string LispStr(Exp exp)
        {
            if (exp.Count > 0)
                return $"( {string.Join(" ", exp.Expr.Select(LispStr))} )";
            return exp.ToString();
        }

        //................ Eval .......................................
        //"Evaluate an expression in an environment."
        static Exp Eval(Exp x, Env env)
        {
            if (env == null)
                env = _globalEnv;

            if (x.IsSym) // variable reference
                return new Exp(env.Find(x.Sym)[x.Sym]);
            else if (x.Count == 0) // constant literal
                return x;
            else if (x[0].Sym == "quote") // (qoute exp)
                return x.Rest;
            else if (x.Expr[0].Sym == "if") // (if test conseq alt)
            {
                var (test, conseq, alt) = (x[1], x[2], x[3]);
                var exp = (bool)Eval(test, env).Bool ? conseq : alt;
                return Eval(exp, env);
            }
            else if (x[0].Sym == "define") // (define var exp)
            {
                var (var, exp) = (x[1], x[2]);
                env[var.Sym] = (_, __) => Eval(exp, env);
                return new Exp("");
            }
            else if (x[0].Sym == "set!")  // (set! var exp)
            {
                var (var, exp) = (x[1], x[2]);
                env.Find(var.Sym)[var.Sym] = (_, __) => Eval(exp, env);
                return new Exp("");
            }
            else if (x[0].Sym == "lambda") // (lambda (var...) body )
            {
                var (parms, body) = (x[1], x[2]);
                return Procedure(parms, body, env);
            }
            else if (x[0].Sym == "begin") // (begin exp+)
            {
                Exp last = null;
                foreach (var exp in x.Rest.Expr)
                    last = Eval(exp, env);
                return last;
            }
            else                                // proc arg
            {
                var proc = Eval(x.Head, env);
                var args = from exp in x.Rest.Expr select Eval(exp, env);
                return proc.Inv(new Exp(args.ToList()), env);
            }
        }

        //"A user-defined Scheme procedure."
        static Exp Procedure(Exp parms, Exp body, Env env) => (Exp args, Env e) => Eval(body, new Env(parms, args, env));
    }

    class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string message) : base(message) { }
    }
}