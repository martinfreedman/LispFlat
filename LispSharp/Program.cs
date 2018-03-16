using System;

namespace LispFlat
{
    class Program
    {
        static void Main(string[] args)
        {
            var env = Lisp.StandardEnv();

            try
            {
                Tests.Run(env);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Read();
            }

            while (true)
            {
                try
                {
                    Lisp.Repl("lisp.cs",env);
                }
                catch (SyntaxErrorException ex)
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
