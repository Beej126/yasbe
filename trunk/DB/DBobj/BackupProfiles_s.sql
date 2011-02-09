
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[BackupProfiles_s]    Script Date: 06/19/2009 15:58:30 ******/
use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'BackupProfiles_s')
	exec('create PROCEDURE BackupProfiles_s as select 1 as one')
GO
alter PROCEDURE [dbo].[BackupProfiles_s] 
@BackupProfileID INT = null
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT
	BackupProfileID,
	Name
FROM BackupProfile

SELECT * FROM dbo.BackupProfileFolder WHERE BackupProfileID = @BackupProfileID

END
GO

grant execute on BackupProfiles_s to public
go

