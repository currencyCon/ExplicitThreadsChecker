using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            new Thread(Compute).Start();  
            
        }

        public void Test2()
        {
            var thread = new Thread(Compute);
            thread.Start();
            thread.Join();

            //Task.Run(() => Compute()); 
        }
        private void Compute()
        {
            int i=10;
            i++;
        }

        private void Test3(Thread ttt)
        {
            int i = 1;
            if (i == 1)
            {
                Thread t, l;
                t = new Thread(Compute);
                t.Start();
                Hallo(t);

                l = new Thread(Compute);
                l.Start();
            }
            else
            {
                Thread t = new Thread(Compute);
                
                t.Start();
                //Hallo(t);
            }
        }

        private void Hallo(Thread thread)
        {
            Thread t = new Thread(Compute);
            t.Start();

            Thread s, l;
            Thread tttt;

        }

        public static void Main()
        {
        }
    }
}
