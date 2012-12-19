CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE customers.`GetPricesAVGCost`(IN UserIdParam INT UNSIGNED)
BEGIN

set @currentDay = usersettings.CurrentDayOfWeek();
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
SELECT  pd.firmcode,
        i.PriceId,
        pc.CostCode,
        ifnull(pd.ParentSynonym, pd.pricecode) PriceSynonymCode,
        i.RegionId,
        0 as DelayOfPayment,
        if(up.PriceId is null, 1, 0),
        round((1 + pd.UpCost / 100) * (1 + prd.UpCost / 100) * (1 + i.PriceMarkup / 100), 5),
        (to_seconds(now()) - to_seconds(pi.PriceDate)) < (f.maxold * 86400),
        pd.CostType,
        pi.PriceDate,
        r.ShowPriceName,
        pd.PriceName,
        pi.RowCount,
        prd.MinReq,
        0,
        (r.OrderRegionMask & i.RegionId & u.OrderRegionMask) > 0,
        supplier.Name as ShortName,
        si.SupplierCategory,
        si.SupplierCategory >= r.BaseFirmCategory,
        Storage,
        dop.VitallyImportantDelay,
        dop.OtherDelay
FROM Customers.Users u
  join Customers.Intersection i on i.ClientId = u.ClientId and i.AgencyEnabled = 1
  JOIN Customers.Clients drugstore ON drugstore.Id = i.ClientId
  JOIN usersettings.RetClientsSet r ON r.clientcode = drugstore.Id
  JOIN usersettings.PricesData pd ON pd.pricecode = i.PriceId
    join usersettings.SupplierIntersection si on si.SupplierId = pd.FirmCode and i.ClientId = si.ClientId
    join usersettings.PriceIntersections pinter on pinter.SupplierIntersectionId = si.Id and pinter.PriceId = pd.PriceCode
    join usersettings.DelayOfPayments dop on dop.PriceIntersectionId = pinter.Id and dop.DayOfWeek = @currentDay
  JOIN usersettings.PricesCosts pc on pc.PriceCode = i.PriceId 
  and ((r.InvisibleOnFirm > 0 and pc.BaseCost = 1) or (r.InvisibleOnFirm = 0 and pc.CostCode = i.CostId and pd.CostType = 0) or (r.InvisibleOnFirm = 0 and pc.BaseCost = 1 and pd.CostType = 1))
    JOIN usersettings.PriceItems pi on pi.Id = pc.PriceItemId
    JOIN farm.FormRules f on f.Id = pi.FormRuleId
    JOIN Customers.Suppliers supplier ON supplier.Id = pd.firmcode
    JOIN usersettings.PricesRegionalData prd ON prd.regioncode = i.RegionId AND prd.pricecode = pd.pricecode
    JOIN usersettings.RegionalData rd ON rd.RegionCode = i.RegionId AND rd.FirmCode = pd.firmcode
  left join Customers.UserPrices up on up.PriceId = i.PriceId and up.UserId = ifnull(u.InheritPricesFrom, u.Id) and up.RegionId = i.RegionId
WHERE   supplier.Disabled = 0
    and (supplier.RegionMask & i.RegionId) > 0
    AND (drugstore.maskregion & i.RegionId & u.WorkRegionMask) > 0
    AND (r.WorkRegionMask & i.RegionId) > 0
    AND pd.agencyenabled = 1
    AND pd.enabled = 1
    AND pd.pricetype <> 1
    AND prd.enabled = 1
    AND if(not r.ServiceClient, supplier.Id != 234, 1)
    and i.AvailableForClient = 1
    AND u.Id = UserIdParam
group by PriceId, RegionId;

END;
