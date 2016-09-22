using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExplicitThreadsSmell
{
    class ThreadWithLambda
    {
        public ThreadWithLambda()
        {
            new Thread(() => { int i = 10; i++;}).Start();
        }
    }
}
