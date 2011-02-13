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
@IncrementalID INT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

UPDATE MediaSubset SET Finalized = 1 
WHERE MediaSubsetID = (SELECT MAX(MediaSubsetID) FROM MediaSubset WHERE IncrementalID = @IncrementalID)

END
GO

grant execute on MediaSubset_Commit to public
go

