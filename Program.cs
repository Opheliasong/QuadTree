using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuadTree_1
{
    class Program
    {
        static void Main(string[] args)
        {
            QuadTree t = new QuadTree(0,0, 16);
            t.Insert(1, 1, 2);
            t.Insert(0, 1, 2);
            t.Insert(1, 0, 2);
            t.Insert(0, 0, 2);
        }
    }
}
