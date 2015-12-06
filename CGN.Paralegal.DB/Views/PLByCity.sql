CREATE VIEW [dbo].[PLByCity]
	AS  SELECT Para_legal.Para_legal_id,
				Para_legal.Para_legal_name,
				Para_legal.[Short desc],
				Location_name
		FROM Para_legal 
		INNER JOIN Para_Legal_Location
		ON Para_legal.Para_legal_id = Para_Legal_Location.Para_legal_id
		INNER JOIN Location
		ON Para_Legal_Location.location_id = Location.Location_id
		
