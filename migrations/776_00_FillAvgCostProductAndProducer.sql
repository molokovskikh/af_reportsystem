drop temporary table IF EXISTS reports.TmpPricesRegions;
CREATE temporary table reports.TmpPricesRegions(
  Assortment int(32) unsigned,
  Product int(32) unsigned,
  Producer int(32) unsigned
  ) engine=MEMORY;
  
  
 insert into reports.TmpPricesRegions
  select a.Id, p.Id, a.ProducerId
from (catalogs.assortment a,
catalogs.products p)
left join catalogs.productproperties pr on pr.productid = p.id
where
a.catalogid=p.catalogid
and (p.hidden=0 or not exists (select * from catalogs.products left join catalogs.productproperties pr1 on pr1.productid = products.id where products.hidden=0 and products.catalogid=p.catalogid and pr1.productid is null))
and pr.productid is null
group by a.Id
order by p.Id;

update reports.averagecosts ac join 
reports.TmpPricesRegions tmp on ac.assortmentid = tmp.Assortment
set ac.ProductId=tmp.Product, ac.ProducerId=tmp.Producer
where ac.assortmentid is not null;