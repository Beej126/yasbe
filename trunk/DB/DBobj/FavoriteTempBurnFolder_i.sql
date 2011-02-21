--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[FavoriteTempBurnFolder_i]    Script Date: 06/19/2009 15:58:30 ******/

/*testing:
exec [FavoriteTempBurnFolder_i] @BackupProfileID=2

DELETE Incremental
DBCC CHECKIDENT (Incremental, RESEED, 0)
*/

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'FavoriteTempBurnFolder_i')
	exec('create PROCEDURE FavoriteTempBurnFolder_i as select 1 as one')
GO
alter PROCEDURE [dbo].[FavoriteTempBurnFolder_i] 
@NewFolder VARCHAR(500)
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

INSERT FavoriteTempBurnFolder ( [Path] ) VALUES (@NewFolder)
SELECT * FROM FavoriteTempBurnFolder

END
GO

grant execute on FavoriteTempBurnFolder_i to public
go

