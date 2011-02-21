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
@BackupProfileID INT,
@Files File_UDT readonly
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE @IncrementalID INT, @MediaSubsetID int
SELECT @IncrementalID = MAX(IncrementalID) FROM [Incremental] WHERE BackupProfileID = BackupProfileID

INSERT MediaSubset ( IncrementalID ) VALUES  ( @IncrementalID )
SET @MediaSubsetID = SCOPE_IDENTITY()

INSERT [File]( FullPath )
SELECT fi.FullPath
FROM @Files fi
WHERE NOT EXISTS(SELECT 1 FROM [File] f WHERE f.FullPath = fi.FullPath)

INSERT FileArchive (MediaSubsetID, FileID, ModifiedDate, [Size])
SELECT @MediaSubsetID, f.FileID, fi.ModifiedDate, fi.[Size]
FROM @Files fi
JOIN [File] f ON f.FullPath = fi.FullPath
        
END
GO

grant execute on MediaSubset_Commit to public
go

