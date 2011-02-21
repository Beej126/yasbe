--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Incremental_FullDetail]    Script Date: 06/19/2009 15:58:30 ******/

/*testing:
exec [Incremental_FullDetail] @BackupProfileID=1, @IncrementalID=null

SELECT MAX(IncrementalID) FROM [Incremental] WHERE BackupProfileID = 1

select * from incremental

14197885567
31562137600



DELETE Incremental
DBCC CHECKIDENT (Incremental, RESEED, 0)
*/

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'Incremental_FullDetail')
	exec('create PROCEDURE Incremental_FullDetail as select 1 as one')
GO
alter PROCEDURE [dbo].[Incremental_FullDetail] 
@BackupProfileID INT,
@IncrementalID INT OUT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

-- if we're just firing up the whole she-bang, pull the most recent Incremental as the default selection for this BackupProfile
IF (@IncrementalID is NULL)
  SELECT @IncrementalID = MAX(IncrementalID) FROM [Incremental] WHERE BackupProfileID = @BackupProfileID

-- return the files resultset first so that we can always assume this is Table[0] whether we call this full detail proc or just the files proc
EXEC Incremental_Files @IncrementalID = @IncrementalID

IF (@BackupProfileID IS NOT NULL) SET @IncrementalID = null

-- if we pass a specific @IncrementalID to Incrementals_s it only gets retrives/computes that one row
-- yeah it's a bit of a hack to reuse some stored proc code
EXEC Incrementals_s @BackupProfileID = @BackupProfileID, @IncrementalID = @IncrementalID



END
GO

grant execute on Incremental_FullDetail to public
go

