using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace MyUtils {
	public class CalculatorCheckException : Exception {
		public int position { get; }
		/*
			message
			position - позиция неправильного символа
		*/
		public CalculatorCheckException(string message, int position)
			: base(message) {
			this.position = position;
		}
	}
	public class Calculator {
		public bool IsDotDecimalSeparator { get; }
		private string RgxS_operators = @"[+\-*/^]";
		private string RgxS_validSymbols;
		public Calculator() {
			if (Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator == ".") {
				IsDotDecimalSeparator = true;
				RgxS_validSymbols = @"[+\-*/0123456789().^]";
			}
			else {
				IsDotDecimalSeparator = false;
				RgxS_validSymbols = @"[+\-*/0123456789(),^]";
			}
		}
		/*
			при ошибке проверки указывать некорректный символ
			пример работы:
			input: (1 - 1) // 1
			showWrongSymol():
				Error:
				4//2
			      ^
		*/
		public int GetWrongSymbolPosition(string s) {
			try {
				Check(s);
				return -1;
			}
			catch (CalculatorCheckException cex) {
				return cex.position;
			}
		}

		/* 
			готовит выражение перед выполнением:
			убирает лишние символы, добавляет "0" в начало, если нужно
		*/
		public string Prepare(string s) {
			// убираем все пробелы
			s = s.Replace(" ", string.Empty);
			if (!IsDotDecimalSeparator) {
				s = s.Replace(".", ",");
			}
			// убираем внешние скобки
			if (s.Length > 1 && s[0] == '(' && s[s.Length - 1] == ')') {
				s = s.Substring(1, s.Length - 2);
			}
			// добавляем "0" в начало, если надо
			if (s == "" || s.Length > 1 && s[0] == '-') {
				s = '0' + s;
			}
			// заменяем все фрагменты чисел с "." в начале (вместо "0.) на "0.)
			for (int i = 0; i < s.Length; i++) {
				if (i > 0 && Regex.IsMatch(s[i - 1].ToString(), @"[^0-9]") && (IsDotDecimalSeparator ? s[i] == '.' : s[i] == ',')) {
					s = s.Insert(i, "0");
				}
			}
			// "10(...)" заменять на "10*(...)"
			// "(...)5" заменять на "(...)*5"
			for (int i = 0; i < s.Length; i++) {
				if (i > 0 && Char.IsDigit(s[i - 1]) && s[i] == '(') {
					s = s.Insert(i, "*");
				}
				else if (i > 0 && Char.IsDigit(s[i]) && s[i - 1] == ')') {
					s = s.Insert(i, "*");
				}
			}
			return s;
		}

		/* проверяет строку на корректность */
		/*
			признаки некорректоности:
				- есть непонятные символы
				- есть два подряд идущих оператора ("++", "-*, "//" и т.п.)
				- содержит число, в котором больше 1 точки (".")
				- ? количество "(" не соответствует числу ")"
		*/
		private bool TryCheck(string s) {
			string prev = "";
			uint openingBracketCount = 0, closingBracketCount = 0;
			uint pointsCount = 0;
			if (s.Length == 0 || Regex.IsMatch(s[0].ToString(), RgxS_operators))
				return false;
			int i = 0;
			for (; i < s.Length; i++) {
				// Если содержит символ является недопустимым
				if (!Regex.IsMatch(s[i].ToString(), RgxS_validSymbols))
					return false;
				// Если строка содержит недопустимую последовательность символов
				if (Regex.IsMatch(s[i].ToString(), @"[+\-*/]") && s[i].ToString() == prev)
					return false;
				// после оператора должна идти цифра, "." или "("
				if (Regex.IsMatch(prev, RgxS_operators) && (!Regex.IsMatch(s[i].ToString(), IsDotDecimalSeparator ? @"[0-9.(]" : @"[0-9,(]")))
					return false;
				if (Regex.IsMatch(s[i].ToString(), RgxS_operators) && i == s.Length - 1)
					return false;
				// Если в числе больше одной "."
				if (IsDotDecimalSeparator ? s[i] == '.' : s[i] == ',') {
					if (pointsCount > 0)
						return false;
					pointsCount++;
				}
				else if (!Char.IsDigit(s[i]) && pointsCount != 0)
					pointsCount = 0;

				if (s[i] == '(') openingBracketCount++;
				else if (s[i] == ')') closingBracketCount++;
				prev = s[i].ToString();
			}
			if (openingBracketCount != closingBracketCount)
				return false;
			return true;
		}

		public bool Check(string s) {
			string prev = "";
			uint openingBracketCount = 0, closingBracketCount = 0;
			uint pointsCount = 0;
			if (s.Length == 0)
				throw new CalculatorCheckException("empty string", -1);
			if (Regex.IsMatch(s[0].ToString(), @"[*/^]"))
				throw new CalculatorCheckException("expression cannot start with operator", 0);
			for (int i = 0; i < s.Length; i++) {
				// Если содержит символ является недопустимым
				if (!Regex.IsMatch(s[i].ToString(), RgxS_validSymbols))
					throw new CalculatorCheckException("invalid symbol", i);
				// Если строка содержит недопустимую последовательность символов
				if (Regex.IsMatch(s[i].ToString(), RgxS_operators) && s[i].ToString() == prev)
					throw new CalculatorCheckException("invalid character sequence", i);
				// после оператора должна идти цифра, "." или "("
				if (Regex.IsMatch(prev, RgxS_operators) && (!Regex.IsMatch(s[i].ToString(), "[0-9.(]")))
					throw new CalculatorCheckException("after the operator there should be a digit, \".\" or \"(\"", i);
				if (Regex.IsMatch(s[i].ToString(), @"[+\-*/]") && i == s.Length - 1)
					throw new CalculatorCheckException("expression cannot end with statement", i);
				// Если в числе больше одной "."
				if (s[i] == '.') {
					if (pointsCount > 0)
						throw new CalculatorCheckException("number contains more than one \".\"", i);
					pointsCount++;
				}
				else if (!Char.IsDigit(s[i]) && pointsCount != 0)
					pointsCount = 0;

				if (s[i] == '(') openingBracketCount++;
				else if (s[i] == ')') closingBracketCount++;
				prev = s[i].ToString();
			}
			// ...
			if (openingBracketCount != closingBracketCount)
				throw new CalculatorCheckException("the number of \"(\" does not match the number of \")\"", -1);
			return true;
		}

		private string PullOperand(string s) {
			string sr = ""; // string result
			if (s.Length == 0)
				return "";
			// if s.begin_fragment - number
			for (int i = 0; i < s.Length; i++) {
				char ch = s[i];
				if (char.IsDigit(ch) || (IsDotDecimalSeparator ? ch == '.' : ch == ',')) {
					sr += ch;
				}
				else break;
			}
			// if  s - (...)
			if (s[0] == '(') {
				sr += '(';
				for (int i = 1; i < s.Length; i++) {
					char ch = s[i];
					if (ch != ')') {
						sr += ch;
					}
					else {
						sr += ch;
						return sr;
					}
				}
			}
			return sr;
		}

		public double Execute(string s) {
			double d = 0;
			List<char> operators = new List<char>(); // операторы
			List<string> s_operands = new List<string>(); // операнды (числа/выражения) в строчной форме
			List<double> d_operands = new List<double>(); // операнды в числовой форме (вычесленные)

			if (s.Length == 0)
				throw new Exception();
			s = Prepare(s);
			if (!TryCheck(s))
				throw new Exception();
			/*
				разбор строки на операнды и операторы
				-> массив операндов и массив операторов
			*/
			for (int i = 0, offset = 1; i < s.Length; i += offset) {
				// if s[i] == (, ), ., 0...9,
				if (Regex.IsMatch(s[i].ToString(), @"[()0-9]")) {
					string rest = s.Substring(i, s.Length - i);
					string operand = PullOperand(rest);
					s_operands.Add(operand);
					offset = operand.Length;
				}
				// if s[i] == +,-,*,/
				else {
					operators.Add(s[i]);
					offset = 1;
				}
			}
			/*
				превращаем все операнды в числа
				-> d_operands done
			*/
			for (int i = 0; i < s_operands.Count; i++) {
				/*
					если operands[i] - число: operand = operands[i]
					иначе (opeands[i] - выражение: operand = calc(operands[i])
				*/
				if (!double.TryParse(s_operands[i], out d)) {
					d_operands.Add(Execute(s_operands[i]));
				}
				else {
					d_operands.Add(d);
				}
			}
			// ! приоритет операторов
			// if operators[i] = @"*/": operands[i] [*/]= operands[i+1]
			// operands.remove(i+1)
			// operators.remove(i)
			for (int i = 0; i < operators.Count; i++) {
				switch (operators[i]) {
					case '*':
						d_operands[i] *= d_operands[i + 1];
						d_operands.RemoveAt(i + 1);
						operators.RemoveAt(i);
						i--;
						break;
					case '/':
						d_operands[i] /= d_operands[i + 1];
						d_operands.RemoveAt(i + 1);
						operators.RemoveAt(i);
						i--;
						break;
					case '^':
						d_operands[i] = Math.Pow(d_operands[i], d_operands[i + 1]);
						d_operands.RemoveAt(i + 1);
						operators.RemoveAt(i);
						i--;
						break;
				}
			}
			// последовательно выполняем все операции
			double result = d_operands[0];
			for (int i = 1; i < d_operands.Count; i++) {
				switch (operators[i - 1]) {
					case '+':
						result += d_operands[i];
						break;
					case '-':
						result -= d_operands[i];
						break;
					case '*':
						result *= d_operands[i];
						break;
					case '/':
						result /= d_operands[i];
						break;
					case '^':
						result = Math.Pow(result, d_operands[i]);
						break;
				}
			}
			return result;
		}
		/* выполняет выражение */
		public bool TryExecute(string s, out double d) {
			d = 0;
			double r; // temp
			List<char> operators = new List<char>(); // операторы
			List<string> s_operands = new List<string>(); // операнды (числа/выражения) в строчной форме
			List<double> d_operands = new List<double>(); // операнды в числовой форме (вычесленные)

			if (s.Length == 0)
				return false;
			s = Prepare(s);
			if (!TryCheck(s))
				return false;
			/*
				разбор строки на операнды и операторы
				-> массив операндов и массив операторов
			*/
			for (int i = 0, offset = 1; i < s.Length; i += offset) {
				// if s[i] == (, ), ., 0...9,
				if (Regex.IsMatch(s[i].ToString(), @"[()0-9]")) {
					string rest = s.Substring(i, s.Length - i);
					string operand = PullOperand(rest);
					s_operands.Add(operand);
					offset = operand.Length;
				}
				// if s[i] == +,-,*,/
				else {
					operators.Add(s[i]);
					offset = 1;
				}
			}
			/*
				превращаем все операнды в числа
				-> d_operands done
			*/
			for (int i = 0; i < s_operands.Count; i++) {
				/*
					если operands[i] - число: operand = operands[i]
					иначе (opeands[i] - выражение: operand = calc(operands[i])
				*/
				if (!double.TryParse(s_operands[i], out r)) {
					d_operands.Add(Execute(s_operands[i]));
				}
				else {
					d_operands.Add(r);
				}
			}
			// ! приоритет операторов
			// if operators[i] = @"*/": operands[i] [*/]= operands[i+1]
			// operands.remove(i+1)
			// operators.remove(i)
			for (int i = 0; i < operators.Count; i++) {
				switch (operators[i]) {
					case '*':
						d_operands[i] *= d_operands[i + 1];
						d_operands.RemoveAt(i + 1);
						operators.RemoveAt(i);
						i--;
						break;
					case '/':
						d_operands[i] /= d_operands[i + 1];
						d_operands.RemoveAt(i + 1);
						operators.RemoveAt(i);
						i--;
						break;
					case '^':
						d_operands[i] = Math.Pow(d_operands[i], d_operands[i + 1]);
						d_operands.RemoveAt(i + 1);
						operators.RemoveAt(i);
						i--;
						break;
				}
			}

			// последовательно выполняем оставшиеся операции операции
			double result = d_operands[0];
			for (int i = 1; i < d_operands.Count; i++) {
				switch (operators[i - 1]) {
					case '+':
						result += d_operands[i];
						break;
					case '-':
						result -= d_operands[i];
						break;
					case '*':
						result *= d_operands[i];
						break;
					case '/':
						result /= d_operands[i];
						break;
					case '^':
						result = Math.Pow(result, d_operands[i]);
						break;
				}
			}
			d = result;
			return true;
		}
	}
}

