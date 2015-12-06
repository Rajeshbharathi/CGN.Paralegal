CREATE VIEW [dbo].[PLByAOP]
	AS  SELECT Para_legal.Para_legal_id,
				Para_legal.Para_legal_name,
				Para_legal.[Short desc],
				Area_of_Law.Law_name
		FROM Para_legal 
		INNER JOIN Para_Legal_Law_Area
		ON Para_legal.Para_legal_id = Para_Legal_Law_Area.Para_legal_id
		INNER JOIN Area_of_Law
		ON Para_Legal_Law_Area.Law_id = Area_of_Law.Law_id
		
