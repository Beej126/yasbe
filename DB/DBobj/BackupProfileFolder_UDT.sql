--$Author: Brent.anderson2 $
--$Date: 11/20/09 4:22p $
--$Modtime: 11/20/09 4:13p $

use YASBE
go

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

/*
sp_procsearch 'BackupProfileFolder_UDT'
drop proc BackupProfile_u
drop TYPE BackupProfileFolder_UDT 
*/
if not exists(select 1 from sys.types where name = 'BackupProfileFolder_UDT')
  create TYPE BackupProfileFolder_UDT AS TABLE
  (
    BackupProfileID int,
    IsExcluded bit,
    FullPath VARCHAR(8000)
  )
go