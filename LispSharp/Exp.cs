using System;
using System.Collections.Generic;
using System.Linq;

namespace LispFlat
{
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
}
