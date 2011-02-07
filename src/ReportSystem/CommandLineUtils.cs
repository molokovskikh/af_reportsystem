using System;
using System.Collections;
using System.Collections.Generic;

namespace Inforoom.Common
{
	/// <summary>
	/// Вспомогательный класс для работы с командной строкой
	/// </summary>
	public class CommandLineUtils : IComparer
	{
		string _key;
		public CommandLineUtils(string Key)
		{
			_key = Key;
		}

		public int Compare(object x, object y)
		{
			if ((x is string) && (y is string))
			{
				string Left = (string)x;
				string Right = (string)y;
				return (Left.StartsWith(Right)) ? 0 : Left.CompareTo(Right);
			}
			throw new ArgumentException("Объекты не являются String");
		}

		private bool ValueStartsWith(string Value)
		{
			return Value.StartsWith(_key, StringComparison.CurrentCultureIgnoreCase);
		}

		/// <summary>
		/// Получить код из параметра формата: <prefix><number>
		/// </summary>
		/// <param name="Prefix">значение префикса</param>
		/// <returns></returns>
		public static string GetCode(string Prefix)
		{
			var c = new CommandLineUtils(Prefix);
			var Val = Array.Find<string>(Environment.GetCommandLineArgs(), c.ValueStartsWith);
			if (!String.IsNullOrEmpty(Val))
				try
				{
					Val = Val.Substring(Prefix.Length);
					return Val;
				}
				catch
				{
				}
			return (-1).ToString();
		}

		public static string GetStr(string Prefix)
		{
			var c = new CommandLineUtils(Prefix);
			var Val = Array.Find<string>(Environment.GetCommandLineArgs(), c.ValueStartsWith);
			if (!String.IsNullOrEmpty(Val))
				try
				{
					Val = Val.Substring(Prefix.Length);
					return Val;
				}
				catch
				{
				}
			return null;
		}


	}
}
