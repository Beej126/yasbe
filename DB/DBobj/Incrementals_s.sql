--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Incrementals_s]    Script Date: 06/19/2009 15:58:30 ******/

/*testing:
exec [Incrementals_s] @BackupProfileID=1
select * from Incremental
select * from MediaSubset
select * from FileArchive where mediasubsetid = 1

SELECT 
  i.IncrementalID, i.BackupDate, 
  COUNT(distinct m.IncrementalID) AS [Discs Defined],
  SUM(fa.[Size]) AS [Total Bytes]
FROM Incremental i
JOIN MediaSubset m ON m.IncrementalID = i.IncrementalID
join FileArchive fa ON fa.MediaSubsetID = m.MediaSubsetID
WHERE i.BackupProfileID = 1
AND (null IS NULL OR i.IncrementalID = 1)
GROUP BY i.IncrementalID, i.BackupDate
ORDER BY i.IncrementalID DESC


DELETE Incremental
DBCC CHECKIDENT (Incremental, RESEED, 0)
*/

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'Incrementals_s')
	exec('create PROCEDURE Incrementals_s as select 1 as one')
GO
alter PROCEDURE [dbo].[Incrementals_s] 
@BackupProfileID INT,
@IncrementalID INT = null
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE @MediaSize BIGINT
SELECT @MediaSize = SizeGB * 1024.0 * 1024.0 * 1024.0
FROM MediaSize s 
JOIN BackupProfile p ON p.MediaSizeID = s.MediaSizeID 
WHERE p.BackupProfileID = @BackupProfileID

SELECT 
  i.IncrementalID, i.BackupDate, 
  max(m.MediaSubsetNumber) AS MaxMediaSubsetNumber,
  SUM(fa.[Size]) AS [Total Bytes],
  (SUM(fa.[Size]) / @MediaSize)+1 AS [Discs Required],
  @MediaSize AS [Media Size (true bytes)]
FROM Incremental i
JOIN MediaSubset m ON m.IncrementalID = i.IncrementalID
join FileArchive fa ON fa.MediaSubsetID = m.MediaSubsetID
WHERE i.BackupProfileID = @BackupProfileID
AND (@IncrementalID IS NULL OR i.IncrementalID = @IncrementalID)
GROUP BY i.IncrementalID, i.BackupDate
ORDER BY i.IncrementalID DESC

END
GO

grant execute on Incrementals_s to public
go

