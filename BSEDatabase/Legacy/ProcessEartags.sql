UPDATE [VLA_BSE].[dbo].[Case]
    SET 
        [EartagCountry] = Substring([EartagHerdmark],0,3),
		[EartagHerdmark] = Substring([EartagHerdmark],3,Len([EartagHerdmark])-1)
    WHERE 
		[EartagHerdmark] LIKE 'UK%'
--		OR
--		[EartagHerdmark] LIKE 'AT%' OR
--		[EartagHerdmark] LIKE 'BE%' OR
--		[EartagHerdmark] LIKE 'DE%' OR
--		[EartagHerdmark] LIKE 'DK%' OR
--		[EartagHerdmark] LIKE 'EL%' OR
--		[EartagHerdmark] LIKE 'ES%' OR
--		[EartagHerdmark] LIKE 'FI%' OR
--		[EartagHerdmark] LIKE 'FR%' OR
--		[EartagHerdmark] LIKE 'IE%' OR
--		[EartagHerdmark] LIKE 'IT%' OR
--		[EartagHerdmark] LIKE 'LU%' OR
--		[EartagHerdmark] LIKE 'NL%' OR
--		[EartagHerdmark] LIKE 'PT%' OR
--		[EartagHerdmark] LIKE 'SE%'


UPDATE [VLA_BSE].[dbo].[CaseRelation]
    SET 
        [EartagCountry] = Substring([EartagHerdmark],0,3),
		[EartagHerdmark] = Substring([EartagHerdmark],3,Len([EartagHerdmark])-1)
    WHERE 
		[EartagHerdmark] LIKE 'UK%'
--		OR
--		[EartagHerdmark] LIKE 'AT%' OR
--		[EartagHerdmark] LIKE 'BE%' OR
--		[EartagHerdmark] LIKE 'DE%' OR
--		[EartagHerdmark] LIKE 'DK%' OR
--		[EartagHerdmark] LIKE 'EL%' OR
--		[EartagHerdmark] LIKE 'ES%' OR
--		[EartagHerdmark] LIKE 'FI%' OR
--		[EartagHerdmark] LIKE 'FR%' OR
--		[EartagHerdmark] LIKE 'IE%' OR
--		[EartagHerdmark] LIKE 'IT%' OR
--		[EartagHerdmark] LIKE 'LU%' OR
--		[EartagHerdmark] LIKE 'NL%' OR
--		[EartagHerdmark] LIKE 'PT%' OR
--		[EartagHerdmark] LIKE 'SE%'
 