drop procedure Orders.CalculateOrders;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE Orders.`CalculateOrders`(IN StartDate Date, IN EndDate Date)
BEGIN
SELECT
    supps.Payer PayerId,
    supps.Name SupplierName,
    r.region,
    round(sum(if(free.ClientPayerId is null, cost * quantity, 0)), 2) OrdersSum
FROM (
    ordershead oh
    join usersettings.pricesdata pd on oh.pricecode = pd.pricecode
    join future.suppliers supps on pd.firmcode = supps.Id
    join farm.regions r on oh.regioncode = r.regioncode
    join orderslist ol on ol.orderid = oh.rowid
    join usersettings.retclientsset rcs on rcs.clientcode = oh.clientcode
    join Future.Users u on u.Id = oh.UserId
    join future.clients cl on cl.id = oh.clientcode
    )
    left join billing.FreeOrders free on free.ClientPayerId = cl.PayerId and free.SupplierPayerId = supps.Payer
where
    oh.writetime between StartDate and EndDate
    and supps.segment = 0
    and rcs.InvisibleOnFirm < 2
    and rcs.ServiceClient = 0
    and u.PayerId <> 921
    and oh.Deleted = 0
    and oh.Submited = 1
    and oh.RegionCode in (1, 2, 2048, 4, 8, 32, 64, 16384, 32768, 65536, 128, 16777216, 33554432, 16)
group by supps.id, r.regioncode
order by 2, 1;
END;
