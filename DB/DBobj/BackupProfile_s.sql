--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/*testing:
*/

/****** Object:  StoredProcedure [dbo].[BackupProfile_s]    Script Date: 06/19/2009 15:58:30 ******/
use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'BackupProfile_s')
	exec('create PROCEDURE BackupProfile_s as select 1 as one')
GO
alter PROCEDURE [dbo].[BackupProfile_s]
@BackupProfileID int
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT * FROM Incremental WHERE BackupProfileID = @BackupProfileID ORDER BY IncrementalID desc

SELECT
	BackupProfileID,
	IsExcluded,
	FullPath
FROM BackupProfileFolder
WHERE BackupProfileID = @BackupProfileID 

END
GO

grant execute on BackupProfile_s to public
go

