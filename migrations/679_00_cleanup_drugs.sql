create temporary table duplicate engine=memory
select DrugId,EAN from Reports.Drugs
group by EAN
having count(*) > 1
;

create temporary table for_delete engine=memory
select DrugId from Reports.Drugs d
where (select count(*) from duplicate dup where dup.EAN = d.EAN and d.DrugId <> dup.DrugId) > 0
;

delete from Reports.Drugs
where DrugId in (select DrugId from for_delete);

alter table Reports.Drugs
add unique key (EAN);
