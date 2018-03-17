using System;

namespace LispFlat
{
    class Program
    {
        static void Main(string[] args)
        {
            Env env = null;

            if (args.Length == 1)
            {
                env = Lisp.StandardEnv();

                try
                {
                    Tests.Run(env);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.Read();
                }
            }

            while (true)
            {
                try
                {
                    Lisp.Repl("lispb",env);
                }
                catch (LispException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.Write("Enter 'y' to exit else return to restart: ");
                    if ('y' == Console.Read())
                        Environment.Exit(0);
                }
            }
        }
    }
}
