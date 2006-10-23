using System;
using System.Collections;
using System.Collections.Generic;

namespace Inforoom.Common
{
	/// <summary>
	/// ��������������� ����� ��� ������ � ��������� �������
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
			throw new ArgumentException("������� �� �������� String");
		}

		private bool ValueStartsWith(string Value)
		{
			return Value.StartsWith(_key, StringComparison.CurrentCultureIgnoreCase);
		}

		/// <summary>
		/// �������� ��� �� ��������� �������: <prefix><number>
		/// </summary>
		/// <param name="Prefix">�������� ��������</param>
		/// <returns></returns>
		public static int GetCode(string Prefix)
		{
			CommandLineUtils c = new CommandLineUtils(Prefix);
			string Val = Array.Find<string>(Environment.GetCommandLineArgs(), c.ValueStartsWith);
			if (!String.IsNullOrEmpty(Val))
				try
				{
					Val = Val.Substring(Prefix.Length);
					return Convert.ToInt32(Val);
				}
				catch
				{
				}
			return -1;
		}

	}
}
