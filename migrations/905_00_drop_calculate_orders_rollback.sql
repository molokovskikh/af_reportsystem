DROP PROCEDURE Orders.CalculateOrders;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE Orders.`CalculateOrders`(IN StartDate Date, IN EndDate Date)
BEGIN
SELECT
    supps.Payer PayerId,
    supps.Name SupplierName,
    r.region,
    round(sum(if(free.ClientPayerId is null, cost * quantity, 0)), 2) OrdersSum,
	count(*) RowCount
FROM (
    ordershead oh
    join usersettings.pricesdata pd on oh.pricecode = pd.pricecode
    join Customers.suppliers supps on pd.firmcode = supps.Id
    join farm.regions r on oh.regioncode = r.regioncode
    join orderslist ol on ol.orderid = oh.rowid
    join usersettings.retclientsset rcs on rcs.clientcode = oh.clientcode
    join Customers.Users u on u.Id = oh.UserId
    join Customers.Addresses adr on oh.AddressId = adr.Id
    )
    left join billing.FreeOrders free on free.ClientPayerId = adr.PayerId and free.SupplierPayerId = supps.Payer
where
    oh.writetime between StartDate and EndDate
    and rcs.InvisibleOnFirm < 2
    and rcs.ServiceClient = 0
    and u.PayerId <> 921
    and oh.Deleted = 0
    and oh.Submited = 1
    and r.CalculateOrders = 1
group by supps.id, r.regioncode
order by supps.Name, supps.Payer, r.Region;
END;
