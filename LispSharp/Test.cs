using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LispFlat.Lisp;
using static System.Console;

namespace LispFlat
{
    public static class Tests
    {
        static Dictionary<string, string> _testAll = new Dictionary<string, string>
        {
            ["(quote (testing 1 (2.0) -3.14e159))"] = "(testing 1 (2) -3.14E+159)",
            ["(+ 2 2)"] = "4",
            ["(+ (* 2 100) (* 1 10))"] = "210",
            ["(if (> 6 5) (+ 1 1) (+ 2 2))"] = "2",
            ["(if (< 6 5) (+ 1 1) (+ 2 2))"] = "4",
            ["(define x 3)"] = "",
            ["x"] = "3",
            ["(+ x x)"] = "6",
            ["(begin (define x 1) (set! x (+ x 1)) (+ x 1))"] = "3",
            ["((lambda (x) (+ x x)) 5)"] = "10",
            ["(define twice (lambda (x) (* 2 x)))"] = "",
            ["(twice 5)"] = "10",
            ["(define compose (lambda (f g) (lambda (x) (f (g x)))))"] = "",
            ["((compose list twice) 5)"] = "(10)",
            ["(define repeat (lambda (f) (compose f f)))"] = "",
            ["((repeat twice) 5)"] = "20",
            ["((repeat (repeat twice)) 5)"] = "80",
            ["(define fact (lambda (n) (if (<= n 1) 1 (* n (fact (- n 1))))))"] = "",
            ["(fact 3)"] = "6",
            ["(fact 50)"] = "3.04140932017134E+64",
            ["(define abs (lambda (n) ((if (> n 0) + -) 0 n)))"] = "",
            ["(list (abs -3) (abs 0) (abs 3))"] = "(3 0 3)",
            ["(not #f)"] = "#t",
            ["(length (list 1 2 3))"] = "3",
            ["(length ())"] = "0",
            ["(null? ())"] = "#t",
            ["(null? (list 0))"] = "#f",
            ["(begin (define a (list 1 2 3 4)) a)"] = "(1 2 3 4)",
            ["(car a)"] = "1",
            ["(cdr a)"] = "(2 3 4)",
            ["(car (cdr (cdr a)))"] = "3",
            [@"(define combine (lambda (f)
            (lambda (x y)
              (if (null? x) (quote ())
                  (f (list (car x) (car y))
                     ((combine f) (cdr x) (cdr y)))))))".Replace("\r\n", "")] = "",
            ["(cons (list 1) (list 2 3))"] = "((1) 2 3)",
            ["(cons 1 (list 2 3))"] = "(1 2 3)",
            ["(define zip (combine cons))"] = "",
            ["(zip (list 1 2 3 4) (list 5 6 7 8))"] = "((1 5) (2 6) (3 7) (4 8))",
            ["(append (list 1) (list 2 3))"] = "(1 2 3)",
            ["(append (list 1 2) (list 3))"] = "(1 2 3)",
            ["((combine append) (list 1 2 3 4) (list 5 6 7 8))"] = "(1 5 2 6 3 7 4 8)",
            [@"(define riff-shuffle (lambda (deck) (begin
               (define take (lambda (n seq) (if (<= n 0) (quote ()) (cons (car seq) (take (- n 1) (cdr seq))))))
               (define drop (lambda (n seq) (if (<= n 0) seq(drop (- n 1) (cdr seq)))))
               (define mid (lambda (seq) (/ (length seq) 2)))
               ((combine append) (take (mid deck) deck) (drop (mid deck) deck)))))".Replace("\r\n","")]= "",
            [ "(riff-shuffle (list 1 2 3 4 5 6 7 8))"]= "(1 5 2 6 3 7 4 8)",
            ["((repeat riff-shuffle) (list 1 2 3 4 5 6 7 8))"]=  "(1 3 5 7 2 4 6 8)",
            ["(riff-shuffle (riff-shuffle (riff-shuffle (list 1 2 3 4 5 6 7 8))))"]="(1 2 3 4 5 6 7 8)"
        };

        //"For each (exp, expected) test case, see if eval(parse(exp)) == expected."
        internal static void Run(Env env = null)
        {
            // var fails = 0;
            foreach (var (x, expected) in _testAll.Select(kvp => (kvp.Key, kvp.Value)))
            {
                ForegroundColor = ConsoleColor.Gray;
                var result = Eval(Parse(x), env);
                var ok = (result.ToString() == expected);
                if (expected != "")
                    Write($"{x} => {result}");
                else
                    Write($"{x} => None");
                if (!ok)
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine($" !! => {expected} ");
                    ForegroundColor = ConsoleColor.Gray;
                }
                else
                    WriteLine();
            }
        }
    }
}
