﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoTest.Controllers;

namespace EcoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller();
            controller.go();
        }
    }
}
