--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[MediaSubset_Commit]    Script Date: 06/19/2009 15:58:30 ******/

/*testing:
exec [MediaSubset_Commit] @BackupProfileID=2

DELETE Incremental
DBCC CHECKIDENT (Incremental, RESEED, 0)
*/

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'MediaSubset_Commit')
	exec('create PROCEDURE MediaSubset_Commit as select 1 as one')
GO
alter PROCEDURE [dbo].[MediaSubset_Commit] 
@IncrementalID INT,
@MediaSubsetNumber INT,
@Files FileArchiveID_UDT readonly
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE @MediaSubsetID int
SELECT @MediaSubsetID = MediaSubsetID FROM MediaSubset WHERE IncrementalID = @IncrementalID AND MediaSubsetNumber = @MediaSubsetNumber
IF (@MediaSubsetID IS NULL)
BEGIN
  INSERT MediaSubset (IncrementalID, MediaSubsetNumber) VALUES (@IncrementalID, @MediaSubsetNumber)
  SELECT @MediaSubsetID = SCOPE_IDENTITY()
END

UPDATE fa SET fa.MediaSubsetID = @MediaSubsetID
FROM @Files f
JOIN FileArchive fa ON fa.FileArchiveID = f.FileArchiveID

UPDATE MediaSubset SET Finalized = 1 WHERE MediaSubsetID = @MediaSubsetID

END
GO

grant execute on MediaSubset_Commit to public
go

