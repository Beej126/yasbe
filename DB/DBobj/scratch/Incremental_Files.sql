--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

/****** Object:  StoredProcedure [dbo].[Incremental_Files]    Script Date: 06/19/2009 15:58:30 ******/

/*testing:
exec [Incremental_Files] @IncrementalID=1
*/
use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

if not exists(select 1 from sysobjects where name = 'Incremental_Files')
	exec('create PROCEDURE Incremental_Files as select 1 as one')
GO
alter PROCEDURE [dbo].[Incremental_Files]
@IncrementalID INT
AS BEGIN
	
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT 
  f.FullPath, fa.ModifiedDate, fa.Size,
  m.Finalized,
  CONVERT(BIT, 0) AS SkipError,
  m.MediaSubsetNumber,
  fa.FileArchiveID--, m.MediaSubsetID
FROM MediaSubset m
JOIN FileArchive fa ON fa.MediaSubsetID = m.MediaSubsetID
JOIN [File] f ON f.FileID = fa.FileID
WHERE m.IncrementalID = @IncrementalID
ORDER BY f.FullPath asc, fa.[Size] DESC

-- strategically sorting by fullpath and descending size should keep files from same folders on sequential media
-- and also tackle the big files up front so that we have better odds of squeezing the smaller ones onto the end of the media

-- our "next disc's worth of files" algorithm will walk down the list adding files until it *goes over* the media size
-- then we'll show the user what we've got ... i.e. they can manually inspect the list and see how close it is to using the media up efficiently
-- i think it makes sense to let them add/subtract files manually at this stage just for full flexibility... not sure but feels "right"
 
-- depending on how close it is, it would generally be more straightforward from a retrieval standpoint to simply stick with 
-- flat alphabetical pathing order as we stream files out to subsequent optical discs
-- (i.e. without looking to cram subsequent smaller files that would still fit)
-- this simply facilitates manual "eyeballing" when looking for an archived file on your optical discs

-- ***but*** we could prompt the user if they want us to keep trying to cram subsequent (smaller) files on the current disc
-- by skipping the most recent one that tipped us over and continuing down the list, going over, skipping, until we hit the end of the list
-- (or somehow manage to nail the exact disc size with our total files :)

END
GO

grant execute on Incremental_Files to public
go

