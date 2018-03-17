# LispFlat
(LispFlat (A C# port of Peter Norvig's Toy Lisp) or (How to Write a (Lisp) Interpreter (in C#))) 

## Introduction ##
This is a quick port to C# of Peter Norvig's famous [lis.py](https://github.com/norvig/pytudes/blob/master/py/lis.py) toy Lips(Scheme) interpretor.

The purpose of this project was, primarily, to build an interpretor again , I have not written one in years. 
I have been considering getting back into Lisp programming and there is no better way than to write an interpretor to remind oneself as to the basics. 

Secondly I enjoy learning about code and, surprisingly, find that it is easier to translate from Python to C# rather than from C or C++, two languages I used to know very well! 
As you will see here, the code quite reasonably mirror's python with some caveats, of course.

Finally the original code is a toy interpretor that is, it is a *toy*. It had very little error checking with very limited functionality (with which you can still go surprisingly far) 
and this project mirrors those limitations too. It is a fun and not a professional project. 
Later, Peter enhanced this with [lisp.py](https://github.com/norvig/pytudes/blob/master/py/lispy.py), I might one day revisit this project and update it to that level, but that is not for today.

##Development##
First of all, I recommend that you read Peter's [http://norvig.com/lispy.html](notes) on his project first.

One of my secondary challenges was to, whist not playing code golf, get my solution  to be as small as possible. I could not hope to reach 
Peter's less than 90 lines of code but this solution is around 180 lines excluding open and close brackets, so going from a dynamic to 
statically typed language (and one with a poor type system and poor type inference compared to, say, F#) I consider this pretty good. I 
have even retained Peter's orignal comments and most of the variable names. Nonetheless this has led to some compromises that make the 
code possibly more pythonic or lispy than C# in some areas, but also there aqre a couple of ugly features that I would have removed if this
was a serious project but it was not worth the time.

The code follows much of the original format except I ahd to add an Exp (Expressions) class to handle the different types of this toy. 
I only had double for numbers (the python has ints as well), strings used as the symbol type, I added booleans (`#t` and `#f`- that are not in the Python) 
and a type to handle the lambdas (I do not use C##s own lambda for this but I do use C~ lambdas eslewhere) and a List<Exp> to recursively store the whole 
Abstract Syntax Tree.

### Expressions class ###
```csharp
 internal class Exp
    {
        enum EType { Num, Bool, Sym, List, Proc }

        public string Sym { get; }
        public double Num { get; }
        public bool Bool { get; }
        public List<Exp> List { get; }
        public Func<Exp, Env, Exp> Inv { get; }
        private EType _eType;

        public Exp(string sym) {Sym = sym; _eType = EType.Sym; }
        public Exp(double dbl) {Num = dbl; _eType = EType.Num; }
        public Exp(bool @bool) {Bool = @bool; _eType = EType.Bool; }
        public Exp(List<Exp> exp) { List = exp; _eType = EType.List; }
        public Exp(IEnumerable<Exp> exp) { List = exp.ToList(); _eType = EType.List; }
        public Exp(Func<Exp, Env, Exp> inv) { Inv = inv; _eType = EType.Proc; }

        public int Count => List?.Count ?? -1;
        public bool IsSym => _eType == EType.Sym;
        public bool IsBool => _eType == EType.Bool;
        public bool IsNum => _eType == EType.Num;
        public bool IsList => _eType == EType.List;
        public bool IsProc => _eType == EType.Proc;

        public Exp Head => List[0];
        public Exp Rest => new Exp(List.Skip(1));
        public Exp this[int i] => List[i];

        public override string ToString()
        {
            if (IsBool)
                return Bool == true ? "#t" : "#f";
            if (IsNum)
                return Num.ToString();
            if (IsSym)
                return Sym;
            if (IsProc)
                return "<function>"; ;
            if (IsList)
                return Count > 0 ? $"({string.Join(" ", List.Select(i => i.ToString()))})" : "";
            return "stringify error";
        }
    }
 ```
 This should be quite clear, but it is still annoying that wedont have discriminant unions (if ever) yet which would have made things at least 
 shorter in this class.
 
 We are using both atoms and lists here and not Scheme's pairs.The main takeout here is that the code uses List<T> as the underlying data structure for those lists. 
 This is obviously inefficient, especially seeing how `Rest` (cdr) is implemented, somethig to revisit should I ever  undetake to implement lisp.py
 
 And now the main body of the code, I will not repeat anything Peter wrote in his original note but will add issues and gotchas that I experienced 
 in the course of performing this port.
 
 ### Environment Dictionary ###
 ```csharp
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
 ```
 This is the environment dictionary structure that holds all the primitive operations, as well as all the user defined operations. It is 
 also used for local function scoping of lambdas - loading up function arguments as needed. I added a minor extra peice of erro checking, 
 it was really useful to see key misses in debugging. 
 (TODO need to check but this looks like a statically scoped solution).
 
 ### (Parse(Tokenize(Read))) ###
 ```csharp
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
 ```
 This is so similar to the python that any python coder completely unfamiliar with C# who knows the orginal code should grok this easily.
 As noted above there is the addition of booleans.
 
 ### Standard Environment ###
 ```csharp
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
 ```
 You will notice there are fewer standard procedures than in the python, but the above was enough to pass all the tests plus add some more tests. 
 I did not try to add in `System.Math` which was a 1 liner in python. There were other procedures I removed that were not tested and did not work 
 in the python such as `map` and `apply`. I added them back in as Lisp as part of the test suite. 
 
 Now there is some real ugliness here and it perturbs my DRY urges, that I have to wrap the both output value of the function delegate 
 in an `Exp` and also the whole of the delegate also in an `Exp`. This is becuase the environment can store non function delegate expressions 
 such as lambda argument parameters and simple variables such as named lists and atoms, so the dictoinary stores Exp as values. This 
 looks like a challange that could be solved with a monad, dealing with wrapping and unwrapping values, unfortunately the types are not 
 generic at this stage. If this was not a toy project, I would definitely revisit this and make it not just cleaner code but it would, 
 likely, be more performant. The above did the job nonetheless. Maybe I ahve missed something obvious, I did not spend much time resolving this issue.
 
 ### REPL ###
 ```csharp
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
 ```
 That should be pretty clear. This `Repl()` is called by program.cs which I will shortly discuss in the debuggin section
 
 ### Eval ###
 ```csharp
         //"Evaluate an expression in an environment."
        internal static Exp Eval(Exp x, Env env=null)
        {
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
 
 ```
This is the most intersting part and closely mirrors the python. Surpsiringly this too very little puzzling to create nor to debug. 
Once I had derived a decent (ish) Exp data structure it was very quick to get a running interpreter. The bulk of the work there was working 
on some issues wiht the standard procedures - that took twice as long as getting the basic skeleton to work. 

The important piece to understand here is how the lambdas work.

TODO explain lambdas

### Testing ###

TODO Add test.cs
TODO add coding issues `(quote ())` `Head.Rest` and so on.
TODO add program.cs

### Conclusion ###

TODO



 
 
