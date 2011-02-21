--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[MediaSize_u]    Script Date: 06/19/2009 15:58:30 ******/

/*testing:
exec [MediaSize_u] @BackupProfileID=2

DELETE Incremental
DBCC CHECKIDENT (Incremental, RESEED, 0)
*/

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'MediaSize_u')
	exec('create PROCEDURE MediaSize_u as select 1 as one')
GO
alter PROCEDURE [dbo].[MediaSize_u] 
@MediaSizeID int,
@Size DECIMAL(9,2)
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

UPDATE MediaSize SET SizeGB = @Size WHERE MediaSizeID = @MediaSizeID

END
GO

grant execute on MediaSize_u to public
go

