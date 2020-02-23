using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace MyUtils {
	class Program {
		static void Main(string[] args) {
			//double d = -(1 + 3.0 / 4 * 8) / (4.0 * 10 * 2) + 7 - 9;
			Calculator calc = new Calculator();

			while (true) {
				Console.WriteLine("=== Calculator ===");
				Console.WriteLine("stop='stop'");
				Console.Write("Enter expression: ");
				string input = Console.ReadLine();
				if (input == "stop")
					break;
				double result;
				if (calc.TryExecute(input, out result)) {
					Console.WriteLine("[" + input + "] -> [" + (input = calc.Prepare(input)) + "] = " + result);
				}
				else {
					Console.WriteLine("invalid input");
				}
				Console.Write("Press any key");
				Console.ReadKey();
				Console.Clear();
			}
		}
	}
}

