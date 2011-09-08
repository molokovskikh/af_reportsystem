DROP PROCEDURE IF EXISTS reports.GetRegionsForClient;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.GetRegionsForClient(in inFilter varchar(255), in inID bigint)
BEGIN
    if(inID is not null) then
        select 
            r.RegionCode as ID,
            r.Region as DisplayValue
        from
            farm.Regions r
        where
            r.RegionCode & inID > 0
        order by r.Region;    
    else
        select
            r.RegionCode as ID,
            r.Region as DisplayValue
        from
            farm.regions r
        order by r.Region;
    end if;    
END
