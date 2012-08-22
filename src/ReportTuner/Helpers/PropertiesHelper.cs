using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using ReportTuner.Models;

namespace ReportTuner.Helpers
{
	public class PropertiesHelper
	{
		private DataTable dtNonOptionalParams;
		private DataTable dtOptionalParams;
		private Report report;
		private IList<ReportProperty> reportProperties;

		public PropertiesHelper(ulong reportCode, DataTable nonOptionalParams, DataTable optionalParams)
		{
			report = Report.TryFind(Convert.ToUInt64(reportCode));
			reportProperties = report.Properties;
			dtNonOptionalParams = nonOptionalParams;
			dtOptionalParams = optionalParams;
		}

		private string CalcMaskRegionForSelectedRegions(ReportProperty priceProp, string [] pricePropNames, string [] regionPropNames)
		{
			if(pricePropNames.Contains(priceProp.PropertyType.PropertyName)) {
				// �������� �������� '������ �������� "�����"'
				var prices = reportProperties.FirstOrDefault(p => pricePropNames.Contains(p.PropertyType.PropertyName));

				decimal regionMask = 0;
				string pricesStr = String.Empty;
				// �������� �������� '������ �������� "�������"'
				var regEqual = reportProperties.FirstOrDefault(p => regionPropNames.Contains(p.PropertyType.PropertyName));
				if(regEqual != null) {
					regionMask = regEqual.Values.Select(v => {
						UInt64 regionCode;
						return UInt64.TryParse(v.Value, out regionCode) ? regionCode : Convert.ToUInt64(0);
					}).Sum(r => Convert.ToDecimal(r));
				}
				if(prices != null) {
					var priceCodes = prices.Values.Select(v => {
						int priceCode;
						if (Int32.TryParse(v.Value, out priceCode))
							return priceCode;
						return -1;
					}).Where(v => v >= 0);
					pricesStr = priceCodes.Implode(",");
				}
				return String.Format("inID={0}&inFilter={1}", Convert.ToUInt64(regionMask), pricesStr);
			}
			return String.Empty;
		}

		private string CalcMaskRegionByClient(ReportProperty regionProp, string [] regionPropNames, string [] clientPropNames)
		{		
			if (regionPropNames.Contains(regionProp.PropertyType.PropertyName))
			{
				// �������� �������� "������"			
				DataRow dr = dtNonOptionalParams.Rows.Cast<DataRow>().Where(r => clientPropNames.Contains(r["PPropertyName"].ToString())).FirstOrDefault();
				if (dr != null)
				{
					using(new SessionScope())
					{
						// ������� ������ ��������
						var regEqual =
							reportProperties.Where(p => p.PropertyType.PropertyName == regionProp.PropertyType.PropertyName).FirstOrDefault();
						if (!(dr["PPropertyValue"] is DBNull))
						{
							uint clientId = Convert.ToUInt32(dr["PPropertyValue"]); // ��� �������				
							Client client = Client.TryFind(clientId);
							if (client != null)
							{
								long clientMaskRegion = client.MaskRegion;
								var regionMask = clientMaskRegion;
								if (regEqual != null)
									regionMask = clientMaskRegion + regEqual.Values
										.Select(v => {
											uint reg;
											if (UInt32.TryParse(v.Value, out reg))
												return reg;
											return 0u;
										})
										.Where(r => r > 0 && (r & clientMaskRegion) == 0).Sum(r => r);
								// ����� ��� ������ ��������, ����������� �������
								return String.Format("inID={0}", regionMask);
								// �������������� �����, �������� ��������� � ����� ��������� ����������� ������� �������
							}
						}
					}
				}
			}
			return String.Empty;
		}

		private string GetUserByClient(ReportProperty selectedProp, string [] suppliersPropNames, string [] clientPropNames, string userPropName)
		{
			if(suppliersPropNames.Contains(selectedProp.PropertyType.PropertyName))
			{
				// �������� �������� "������������" (���� �������)
				DataRow drUser = dtOptionalParams.Rows.Cast<DataRow>().Where(r => r["OPPropertyName"].ToString() == userPropName).FirstOrDefault();
				uint? userid = null;
				if (drUser != null)
				{
					if (!(drUser["OPPropertyValue"] is DBNull))
					{
						userid = Convert.ToUInt32(drUser["OPPropertyValue"]);
					}
				}
				else
				{
					// �������� �������� "������"
					var drClient = dtNonOptionalParams.Rows.Cast<DataRow>().FirstOrDefault(r => clientPropNames.Contains(r["PPropertyName"].ToString()));
					if (drClient != null)
					{
						using (new SessionScope())
						{
							if (!(drClient["PPropertyValue"] is DBNull))
							{
								uint clientId = Convert.ToUInt32(drClient["PPropertyValue"]); // ��� �������				
								Client client = Client.TryFind(clientId);
								if(client != null)
								{
									var user = client.Users.FirstOrDefault();
									if (user != null)
										userid = user.Id;
								}
							}
						}
					}
				}
				if(userid != null) return String.Format("userId={0}", userid);
			}
			return String.Empty;
		}

		public string GetRelativeValue(ReportProperty prop)
		{
			if (report == null) return null;		
			if (report.ReportType.ReportClassName.Contains("PharmacyMixedReport"))
			{
				// � ��������� ��� ������ ������ � ������ �������� ������ ���������� ������ ��������� ������� ������� (� ����� ��, ������� ����� ���� ��������, ����� �� ����� ���� ���������)
				var res = CalcMaskRegionByClient(prop, new[] {"RegionEqual", "RegionNonEqual"}, new[] {"SourceFirmCode"});
				if (!String.IsNullOrEmpty(res)) return res;
			}	
			if (report.ReportType.ReportClassName.Contains("SpecReport"))
			{
				// � ����������� ������ � ������ �������� ������ ���������� ������ ��������� ������� ������� (� ����� ��, ������� ����� ���� ��������, ����� �� ����� ���� ���������)
				var res = CalcMaskRegionByClient(prop, new[] {"RegionClientEqual"}, new[] {"ClientCode"});
				if (!String.IsNullOrEmpty(res)) return res;
				// � ����������� ������ ������ ����������� ������ ������������� � ������ ���������� �������
				res = GetUserByClient(prop, new[] {"IgnoredSuppliers", "FirmCodeEqual"}, new[] {"ClientCode"}, "UserCode");
				if (!String.IsNullOrEmpty(res)) return res;
				// � ����������� ������ ��� ������������ ����� '�� ������� �����' � ������ �����-������ (������ �������� "�����") ������ ������������ ������ ������, ��������� � ����� '������ �������� "�������"'
				res = CalcMaskRegionForSelectedRegions(prop, new[] {"PriceCodeEqual"}, new[] {"RegionEqual"});
				// ��������� ���������� �� ���� ������
				var resTypes = String.Format("&inTypes={0},{1}", 1, 2);
				if (!String.IsNullOrEmpty(res)) return res+resTypes;
				// ������ ���������� "�����"
				res = CalcMaskRegionForSelectedRegions(prop, new[] {"PriceCodeNonValues"}, new[] {"RegionEqual"});
				if (!String.IsNullOrEmpty(res)) return res+resTypes;
			}
			if(report.ReportType.ReportClassName.Contains("CombReport"))
			{
				var res = GetUserByClient(prop, new[] { "IgnoredSuppliers", "FirmCodeEqual" }, new[] { "ClientCode" }, "UserCode");
				if (!String.IsNullOrEmpty(res)) return res;
				// � ��������������� ������ ��� ������������ ����� '�� ������� �����' � ������ �����-������ (������ �������� "�����") ������ ������������ ������ ������, ��������� � ����� '������ �������� "�������"'
				res = CalcMaskRegionForSelectedRegions(prop, new[] {"PriceCodeEqual"}, new[] {"RegionEqual"});
				if (!String.IsNullOrEmpty(res)) return res;
			}
			if (report.ReportType.ReportClassName.Contains("DefReport"))
			{
				var res = GetUserByClient(prop, new[] { "IgnoredSuppliers" }, new[] { "ClientCode" }, "UserCode");
				if (!String.IsNullOrEmpty(res)) return res;
				// � ����������� ������ ��� ������������ ����� '�� ������� �����' � ������ �����-������ (������ �������� "�����") ������ ������������ ������ ������, ��������� � ����� '������ �������� "�������"'
				res = CalcMaskRegionForSelectedRegions(prop, new[] {"PriceCodeEqual"}, new[] {"RegionEqual"});
				if (!String.IsNullOrEmpty(res)) return res;
			}
			if (report.ReportType.ReportClassName.Contains("LeakOffersReport"))
			{
				var res = GetUserByClient(prop, new[] { "IgnoredSuppliers", "FirmCodeEqual" }, new[] { "ClientCode" }, "UserCode");
				if (!String.IsNullOrEmpty(res)) return res;
			}
			if (report.ReportType.ReportClassName.Contains("OffersReport"))
			{
				var res = GetUserByClient(prop, new[] { "IgnoredSuppliers", "FirmCodeEqual" }, new[] { "ClientCode" }, "UserCode");
				if (!String.IsNullOrEmpty(res)) return res;
				res = CalcMaskRegionForSelectedRegions(prop, new[] {"PriceCodeEqual"}, new[] {"RegionEqual"});
				if (!String.IsNullOrEmpty(res)) return res;
			}
			if (report.ReportType.ReportClassName.Contains("PharmacyOffersReport"))
			{
				var res = GetUserByClient(prop, new[] { "IgnoredSuppliers", "FirmCodeEqual" }, new[] { "ClientCode" }, "UserCode");
				if (!String.IsNullOrEmpty(res)) return res;
			}
			return String.Empty;
		}
	}
}