using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;

namespace Inforoom.ReportSystem
{
	//¬спомогательный отчет, создаваемый по заказу поставщиков
	public class ProviderReport : BaseReport
	{
		public ProviderReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{ 
		}

		public override void GenerateReport(ExecuteArgs e)
		{ 
		}

		public override void ReportToFile(string FileName)
		{ }

		//ѕолучили список действующих прайс-листов дл€ интересующего клиента, должен существовать параметр "ClientCode"
		protected void GetActivePricesT(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS ActivePricesT;
create temporary table
ActivePricesT
(
 ID SMALLINT unsigned auto_increment primary key,
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 AlowInt Bit,
 Fresh Bit,
 Upcost decimal(7,5),
 MaxSynonymCode Int Unsigned,
 MaxSynonymFirmCrCode Int Unsigned,
 DisabledByClient Bit,
 Actual bit,
 CostType bit,
 PublicCostCorr decimal(7,5),
 DateCurPrice Datetime,
 Region VarChar(50),
 FirmName VarChar(100),
 PosCount int unsigned,
 FirmCategory int unsigned,
 key FirmCode(FirmCode, ID),
 key PriceCode(PriceCode, ID),
 key DisabledByClient(DisabledByClient, ID),
 key Actual(Actual, ID),
 key Fresh(Fresh, ID),
 key CostCode(CostCode, ID) ,
 key CostType(CostType, ID),
 key PriceSynonymCode(PriceSynonymCode, ID),
 key MaxSynonymCode(MaxSynonymCode, ID),
 key MaxSynonymFirmCrCode(MaxSynonymFirmCrCode, ID)
)engine=MEMORY PACK_KEYS = 0;";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			e.DataAdapter.SelectCommand.CommandText = @"
insert into ActivePricesT
SELECT  null,
        pricesdata.firmcode,
        intersection.pricecode,
        intersection.costcode,
        ifnull(ParentSynonym, pricesdata.pricecode) PriceSynonymCode,
        Intersection.RegionCode,
        AlowInt,
        iui.lastsent< DateLastForm,
        (1+pricesdata.UpCost/100)*(1+pricesregionaldata.UpCost/100) *(1+(intersection.FirmCostCorr+intersection.PublicCostCorr)/100),
        iui.MaxSynonymCode,
        iui.MaxSynonymFirmCrCode,
        DisabledByClient,
        to_days(now())-to_days(formrules.datecurprice)< formrules.maxold,
        pricesdata.CostType,
        intersection.PublicCostCorr,
        date_sub(if(datelastform > DateCurPrice, DateCurPrice, DatePrevPrice), interval time_to_sec(date_sub(now(), interval unix_timestamp() second)) second),
        regions.region,
        concat(clientsdata.ShortName, '(', pricesdata.PriceName, ') - ', farm.regions.Region) as FirmName,
        formrules.PosNum,
        intersection.FirmCategory
FROM    intersection,
        clientsdata,
        pricesdata,
        pricesregionaldata,
        retclientsset,
        clientsdata as AClientsData,
        farm.formrules,
        intersection_update_info iui,
        farm.regions
WHERE   DisabledByAgency                                            = 0
        and intersection.clientcode                                 = ?ClientCode
        and retclientsset.clientcode                                = intersection.clientcode
        and formrules.firmcode                                      = pricesdata.pricecode
        and pricesdata.pricecode                                    = intersection.pricecode
        and clientsdata.firmcode                                    = pricesdata.firmcode
        and clientsdata.firmstatus                                  = 1
        and clientsdata.firmtype                                    = 0
        and clientsdata.firmsegment                                 = AClientsData.firmsegment
        and pricesregionaldata.regioncode                           = intersection.regioncode
        and pricesregionaldata.pricecode                            = pricesdata.pricecode
        and AClientsData.firmcode                                   = intersection.clientcode
        and (clientsdata.maskregion & intersection.regioncode)      > 0
        and (AClientsData.maskregion & intersection.regioncode)     > 0
        and (retclientsset.workregionmask & intersection.regioncode)> 0
        and pricesdata.agencyenabled                                = 1
        and pricesdata.enabled                                      = 1
        and invisibleonclient                                       = 0
        and pricesdata.pricetype                                   <> 1
        and pricesregionaldata.enabled                              = 1
        and iui.clientcode                                          = ?ClientCode
        and iui.pricecode                                           = intersection.pricecode
        and iui.regioncode                                          = intersection.regioncode
        and regions.regioncode                                      = intersection.regioncode;";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.Add("ClientCode", (int)_reportParams["ClientCode"]);
			e.DataAdapter.SelectCommand.ExecuteNonQuery();

		}

		//ѕолучили список предложений дл€ интересующего клиента, должен существовать параметр "ClientCode"
		protected void GetAllCoreT(ExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = @"select FirmSegment from usersettings.clientsdata where FirmCode = ?ClientCode";
			e.DataAdapter.SelectCommand.Parameters.Clear();
			e.DataAdapter.SelectCommand.Parameters.Add("ClientCode", (int)_reportParams["ClientCode"]);
			int ClientSegment = Convert.ToInt32(e.DataAdapter.SelectCommand.ExecuteScalar());
			e.DataAdapter.SelectCommand.CommandText = @"
drop temporary table IF EXISTS AllCoreT;
create temporary table AllCoreT
(
 RowID bigint(20) unsigned,
 PriceCode int unsigned,
 RegionCode int unsigned,
 FullCode int unsigned,
 ShortCode int unsigned,
 CodeFirmCr int unsigned,
 SynonymCode int unsigned,
 SynonymFirmCrCode int unsigned,
 Code varchar(32) not null default '                ',
 CodeCr varchar(32) not null default '                ',
 Unit varchar(15) not null default '',
 Volume varchar(15) not null default '',
 Junk bit,
 Await bit,
 Quantity varchar(15) not null default '',
 Note varchar(50)not null default '',
 Period varchar(20) not null default '',
 Doc varchar(20) not null default '',
 RegistryCost decimal(8,2) not null default 0,
 VitallyImportant bit, #-Ќовое поле
 RequestRatio SMALLINT unsigned not null default 0,
 MinCost decimal(8,2), #-Ќовое поле
 Cost decimal(8,2),
 key MultiK(Cost, FullCode, RegionCode),
 key PriceCode(PriceCode)
# key RegionCode(RegionCode, RowID),
# key FullCode(FullCode, RowID),
# key Cost(Cost, ID)
# key MinCost(Cost, RowID),
# key id(id, RowID)
 )engine=MEMORY
 PACK_KEYS = 0;
";
			e.DataAdapter.SelectCommand.ExecuteNonQuery();
			if (ClientSegment == 0)
			{
				e.DataAdapter.SelectCommand.CommandText = @"
INSERT
INTO    AllCoreT
SELECT
        core0.id,
        ActivePricesT.PriceCode,
        ActivePricesT.regioncode,
        core0.fullcode,
        core0.Shortcode,
        codefirmcr,
        synonymcode,
        SynonymFirmCrCode,
        code,
        codecr,
        unit,
        volume,
        length(junk) >0,
        length(Await)>0,
        quantity,
        note,
        period,
        doc,
        RegistryCost,
        VitallyImportant,
        RequestRatio,
        MinBoundCost,
        round(BaseCost*ActivePricesT.UpCost,2)
FROM    farm.core0,
        ActivePricesT
WHERE   core0.firmcode = ActivePricesT.CostCode
        AND not ActivePricesT.AlowInt
        AND not ActivePricesT.DisabledByClient
        AND ActivePricesT.Actual
        AND BaseCost is not null
        AND ActivePricesT.CostType=1;
INSERT
INTO    AllCoreT
SELECT
        core0.id,
        ActivePricesT.PriceCode,
        ActivePricesT.regioncode,
        core0.fullcode,
        core0.Shortcode,
        codefirmcr,
        synonymcode,
        SynonymFirmCrCode,
        code,
        codecr,
        unit,
        volume,
        length(junk) >0,
        length(Await)>0,
        quantity,
        note,
        period,
        doc,
        RegistryCost,
        VitallyImportant,
        RequestRatio,
        MinBoundCost,
        round(corecosts.cost*ActivePricesT.UpCost,2)
FROM    farm.core0,
        ActivePricesT,
        farm.corecosts
WHERE   core0.firmcode = ActivePricesT.PriceCode
        AND not ActivePricesT.AlowInt
        AND not ActivePricesT.DisabledByClient
        AND ActivePricesT.Actual
        AND corecosts.cost is not null
        AND corecosts.Core_Id=core0.id
        and corecosts.PC_CostCode=ActivePricesT.CostCode
        AND ActivePricesT.CostType=0;
UPDATE AllCoreT
        SET cost =MinCost
WHERE   MinCost  >cost
        AND MinCost is not null;
";
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}
			else
			{
				e.DataAdapter.SelectCommand.CommandText = @"
INSERT
INTO    AllCoreT
SELECT  core1.id,
        ActivePricesT.PriceCode,
        ActivePricesT.regioncode,
        core1.fullcode,
        core1.Shortcode,
        codefirmcr,
        synonymcode,
        SynonymFirmCrCode,
        code,
        codecr,
        unit,
        volume,
        length(junk) >0,
        length(Await)>0,
        quantity,
        note,
        period,
        doc,
        RegistryCost,
        VitallyImportant,
        RequestRatio,
        MinBoundCost,
        round(BaseCost*ActivePricesT.UpCost,2)
FROM    farm.core1,
        ActivePricesT
WHERE   core1.firmcode = ActivePricesT.CostCode
        AND not ActivePricesT.AlowInt
        AND not ActivePricesT.DisabledByClient
        AND ActivePricesT.Actual
        AND BaseCost is not null;
UPDATE AllCoreT
        SET cost =MinCost
WHERE   MinCost  >cost
        AND MinCost is not null;
";
				e.DataAdapter.SelectCommand.ExecuteNonQuery();
			}
		}
	}
}
