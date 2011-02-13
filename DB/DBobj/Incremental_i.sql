--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Incremental_i]    Script Date: 06/19/2009 15:58:30 ******/

/*testing:
exec [Incremental_i] @BackupProfileID=2

DELETE Incremental
DBCC CHECKIDENT (Incremental, RESEED, 0)
*/

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'Incremental_i')
	exec('create PROCEDURE Incremental_i as select 1 as one')
GO
alter PROCEDURE [dbo].[Incremental_i] 
@BackupProfileID int,
@IncrementalID int = NULL OUT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

INSERT [Incremental] ( BackupProfileID )
VALUES  ( @BackupProfileID )

SELECT @IncrementalID = SCOPE_IDENTITY()

EXEC Incrementals_s @BackupProfileID = @BackupProfileID

END
GO

grant execute on Incremental_i to public
go

