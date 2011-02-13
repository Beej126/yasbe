
--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Files_UploadCompare]    Script Date: 06/19/2009 15:58:30 ******/
use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'Files_UploadCompare')
	exec('create PROCEDURE Files_UploadCompare as select 1 as one')
GO
alter PROCEDURE [dbo].[Files_UploadCompare]
@BackupProfileID int,
@Files File_UDT readonly
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

/*
INSERT [File] ( FullPath )
SELECT FullPath FROM @Files

INSERT FileArchive ( MediaSubsetID ,FileID ,ModifiedDate ,Size )
SELECT 1, f.FileID, f2.ModifiedDate, f2.size FROM [File] f JOIN @files f2 ON f2.fullpath = f.FullPath
*/

SELECT * FROM @Files f1 WHERE NOT EXISTS(
  SELECT 1
  FROM Incremental i
  JOIN MediaSubset d ON d.IncrementalID = i.IncrementalID
  JOIN FileArchive fa ON fa.MediaSubsetID = d.MediaSubsetID
  JOIN [File] f2 ON f2.FileID = fa.FileID
  WHERE i.BackupProfileID = @BackupProfileID
  AND f2.FullPath = f1.FullPath
  AND fa.ModifiedDate = f1.ModifiedDate
  AND fa.Size = f1.Size
)

END
GO

grant execute on Files_UploadCompare to public
go

