DROP PROCEDURE IF EXISTS future.`GetPricesWithBaseCosts`;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE future.`GetPricesWithBaseCosts`()
BEGIN

drop temporary table IF EXISTS Usersettings.Prices;
create temporary table
Usersettings.Prices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 DelayOfPayment decimal(5,3),
 DisabledByClient bool,
 Upcost decimal(7,5),
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint Unsigned,
 ControlMinReq bool,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 VitallyImportantDelay decimal(5,3),
 OtherDelay decimal(5,3),
 index (PriceCode),
 index (RegionCode)
)engine = MEMORY;

INSERT
INTO    Usersettings.Prices
SELECT  
    pd.firmcode,
    pd.PriceCode as PriceId,
    pc.CostCode,
    ifnull(pd.ParentSynonym, pd.pricecode) PriceSynonymCode,
    prd.RegionCode as RegionId,
    0 as DelayOfPayment,       
    0,
    round((1 + pd.UpCost / 100) * (1 + prd.UpCost / 100), 5), 
    (to_seconds(now()) - to_seconds(pi.PriceDate)) < (f.maxold * 86400),
    pd.CostType,
    pi.PriceDate,
    1,
    pd.PriceName,
    pi.RowCount,
    prd.MinReq, 
    0,
    0,
    supplier.Name as ShortName,
    0,
    0,
    Storage, 
    0,
    0
FROM 
    usersettings.TmpPricesRegions TPR 
    JOIN usersettings.PricesData pd ON TPR.PriceCode = pd.PriceCode
    JOIN usersettings.PricesCosts pc on pc.PriceCode = pd.PriceCode and pc.BaseCost = 1
    JOIN usersettings.PriceItems pi on pi.Id = pc.PriceItemId
    JOIN farm.FormRules f on f.Id = pi.FormRuleId
    JOIN Future.Suppliers supplier ON supplier.Id = pd.firmcode
    JOIN usersettings.PricesRegionalData prd ON prd.pricecode = pd.pricecode AND prd.RegionCode = TPR.RegionCode
    JOIN usersettings.RegionalData rd ON  rd.RegionCode = prd.RegionCode and rd.FirmCode = pd.firmcode
WHERE  
    supplier.Disabled = 0
    and (supplier.RegionMask & prd.RegionCode) > 0
    AND pd.agencyenabled = 1
    AND pd.enabled = 1
    AND pd.pricetype <> 1
    AND prd.enabled = 1
group by PriceId, RegionId;

END
