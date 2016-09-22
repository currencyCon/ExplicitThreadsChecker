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

            Task.Run(() => Compute()); 
        }
        private void Compute()
        {
            int i=10;
            i++;
        }
    }
}
