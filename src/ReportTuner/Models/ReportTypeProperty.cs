﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace ReportTuner.Models
{
	[ActiveRecord("property_enums", Schema = "reports")]
	public class PropertyEnum
	{
		public PropertyEnum()
		{
			Values = new List<EnumValue>();
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property("EnumName")]
		public string Name { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.All, Inverse = true)]
		public IList<EnumValue> Values { get; set; }

		public void AddValue(string name, int value)
		{
			Values.Add(new EnumValue(name, value) {
				Enum = this
			});
		}
	}

	[ActiveRecord("enum_values", Schema = "reports")]
	public class EnumValue
	{
		public EnumValue()
		{}

		public EnumValue(string name, int value)
		{
			DisplayValue = name;
			Value = value;
		}

		[PrimaryKey]
		public uint Id { get; set;}

		[BelongsTo("PropertyEnumId")]
		public PropertyEnum Enum { get; set; }

		[Property]
		public int Value { get; set; }

		[Property]
		public string DisplayValue { get; set; }
	}

	[ActiveRecord("report_type_properties", Schema = "reports")]
	public class ReportTypeProperty : ActiveRecordLinqBase<ReportTypeProperty>
	{
		public ReportTypeProperty()
		{}

		public ReportTypeProperty(string name, string type, string displayName)
		{
			PropertyName = name;
			DisplayName = displayName;
			PropertyType = type;
			if (type.ToLowerInvariant() == "enum")
			{
				Enum = new PropertyEnum();
				Enum.Name = name;
			}

			if (String.Equals(type, "bool", StringComparison.InvariantCultureIgnoreCase))
			{
				DefaultValue = "0";
			}
		}

		[PrimaryKey]
		public virtual ulong Id { get; set; }

		[BelongsTo("ReportTypeCode")]
		public virtual ReportType ReportType { get; set; }

		[Property]
		public virtual string PropertyName { get; set; }

		[Property]
		public virtual string DisplayName { get; set; }

		[Property]
		public virtual string PropertyType { get; set; }

		[Property]
		public virtual bool Optional { get; set; }

		[Property]
		public virtual string DefaultValue { get; set; }

		[BelongsTo("PropertyEnumId", Cascade = CascadeEnum.All)]
		public virtual PropertyEnum Enum { get; set; }
	}
}
