DROP PROCEDURE IF EXISTS future.`GetOffersReports` ;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE future.`GetOffersReports`(IN UserIdParam INT UNSIGNED, IN NoiseFirmCode INT UNSIGNED)
BEGIN

Declare TableExsists Bool DEFAULT false;
Declare FCO INT UNSIGNED DEFAULT 0;
DECLARE CONTINUE HANDLER FOR 1146
if not TableExsists then
call Future.GetActivePrices(UserIdParam);
set TableExsists=true;
end if;
SELECT NULL FROM Usersettings.ActivePrices limit 0;

DROP TEMPORARY TABLE IF EXISTS Usersettings.Core, Usersettings.MinCosts;

CREATE TEMPORARY TABLE Usersettings.Core (
PriceCode INT unsigned,
RegionCode bigint unsigned,
ProductId INT unsigned,
Cost DECIMAL(8,2) unsigned,
CryptCost VARCHAR(32) NOT NULL,
id bigint unsigned,
INDEX (id),
INDEX (PriceCode),
INDEX (ProductId),
INDEX (ProductId, RegionCode, Cost),
INDEX (RegionCode, id)
)engine=MEMORY ;

CREATE TEMPORARY TABLE Usersettings.MinCosts (
MinCost DECIMAL(8,2) unsigned,
ProductId INT unsigned,
regionCode bigint unsigned,
PriceCode INT unsigned,
id bigint unsigned,
UNIQUE  MultiK(ProductId, RegionCode, MinCost),
INDEX (id)
)engine=MEMORY;

INSERT
INTO    Usersettings.Core
SELECT
        straight_join
        Prices.PriceCode,
        Prices.RegionCode,
        c.ProductId,
        if(if(round(cc.Cost * Prices.Upcost, 2) < MinBoundCost, MinBoundCost, round(cc.Cost * Prices.Upcost, 2)) > MaxBoundCost,
        MaxBoundCost, if(round(cc.Cost*Prices.UpCost,2) < MinBoundCost, MinBoundCost, round(cc.Cost * Prices.Upcost, 2))),
        '',
        c.id
FROM Usersettings.ActivePrices Prices
  JOIN farm.core0 c on c.PriceCode = Prices.PriceCode
    JOIN farm.CoreCosts cc on cc.Core_Id = c.Id and cc.PC_CostCode = Prices.CostCode;

Delete from Usersettings.Core where Cost < 0.01;


if ((select FirmCodeOnly from Usersettings.retclientsset join future.Users on Users.ClientId = retclientsset.ClientCode where Users.Id = UserIdParam) is not null) or (NoiseFirmCode is not null) then

if (NoiseFirmCode is not null) then
Select NoiseFirmCode INTO FCO;
ELSE
   select retclientsset.FirmCodeOnly from Usersettings.retclientsset
    join future.Users on Users.ClientId = retclientsset.ClientCode
    where Users.Id = UserIdParam into FCO;
end if;

  update
    Future.Users
    inner join Usersettings.retclientsset on RetClientsSet.clientcode = Users.ClientId
    inner join Usersettings.pricesdata on pricesdata.FirmCode != FCO
    inner join Usersettings.core on Core.PriceCode = PricesData.PriceCode
  set
    core.cost = (1 + (rand() * if(rand() > 0.5, 2, - 2)/100)) * core.cost
  where
    Users.Id = UserIdParam;

end if;

INSERT INTO Usersettings.MinCosts(MinCost, ProductId, RegionCode)
SELECT
        min(Cost),
        ProductId,
        RegionCode
FROM Usersettings.Core
GROUP BY ProductId, RegionCode;

UPDATE Usersettings.MinCosts, Usersettings.Core
SET MinCosts.ID = Core.ID,
    MinCosts.PriceCode = Core.PriceCode
WHERE Core.ProductId = MinCosts.ProductId
      and Core.RegionCode = MinCosts.RegionCode
      and Core.Cost = MinCosts.MinCost;

END ;