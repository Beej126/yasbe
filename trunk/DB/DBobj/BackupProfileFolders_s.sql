
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[BackupProfileFolders_s]    Script Date: 06/19/2009 15:58:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'BackupProfileFolders_s')
	exec('create PROCEDURE BackupProfileFolders_s as select 1 as one')
GO
alter PROCEDURE [dbo].[BackupProfileFolders_s] 
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT
	f.BackupProfileID,
	f.FullPath
FROM BackupProfile p
join BackupProfileFolders f on f.BackupProfileID = p.BackupProfileID

END
GO

grant execute on BackupProfileFolders_s to public
go

