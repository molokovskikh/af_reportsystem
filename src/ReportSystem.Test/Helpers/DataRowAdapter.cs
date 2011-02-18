using System;
using System.Data;

namespace ReportSystem.Test.Helpers
{
	public class DataRowAdapter : IDataRecord
	{
		#region Members
		private DataRow _Row;
		#endregion

		#region Properties
		public DataRow Row
		{
			get { return _Row; }
		}
		#endregion

		#region Constructors
		public DataRowAdapter(DataRow row)
		{
			_Row = row;
		}
		#endregion

		#region IDataRecord Implementation
		public object this[string name]
		{
			get { return _Row[name]; }
		}

		public object this[int i]
		{
			get { return _Row[i]; }
		}

		public int FieldCount
		{
			get { return _Row.Table.Columns.Count; }
		}

		public bool GetBoolean(int i)
		{
			return Convert.ToBoolean(_Row[i]);
		}

		public byte GetByte(int i)
		{
			return Convert.ToByte(_Row[i]);
		}

		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			throw new NotSupportedException("GetBytes is not supported.");
		}

		public char GetChar(int i)
		{
			return Convert.ToChar(_Row[i]);
		}

		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			throw new NotSupportedException("GetChars is not supported.");
		}

		public IDataReader GetData(int i)
		{
			throw new NotSupportedException("GetData is not supported.");
		}

		public string GetDataTypeName(int i)
		{
			return _Row[i].GetType().Name;
		}

		public DateTime GetDateTime(int i)
		{
			return Convert.ToDateTime(_Row[i]);
		}

		public decimal GetDecimal(int i)
		{
			return Convert.ToDecimal(_Row[i]);
		}

		public double GetDouble(int i)
		{
			return Convert.ToDouble(_Row[i]);
		}

		public Type GetFieldType(int i)
		{
			return _Row[i].GetType();
		}

		public float GetFloat(int i)
		{
			return Convert.ToSingle(_Row[i]);
		}

		public Guid GetGuid(int i)
		{
			return (Guid)_Row[i];
		}

		public short GetInt16(int i)
		{
			return Convert.ToInt16(_Row[i]);
		}

		public int GetInt32(int i)
		{
			return Convert.ToInt32(_Row[i]);
		}

		public long GetInt64(int i)
		{
			return Convert.ToInt64(_Row[i]);
		}

		public string GetName(int i)
		{
			return _Row.Table.Columns[i].ColumnName;
		}

		public int GetOrdinal(string name)
		{
			return _Row.Table.Columns.IndexOf(name);
		}

		public string GetString(int i)
		{
			return _Row[i].ToString();
		}

		public object GetValue(int i)
		{
			return _Row[i];
		}

		public int GetValues(object[] values)
		{
			values = _Row.ItemArray;
			return _Row.ItemArray.GetLength(0);
		}

		public bool IsDBNull(int i)
		{
			return Convert.IsDBNull(_Row[i]);
		}
		#endregion
	}
}