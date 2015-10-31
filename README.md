# yasbe = Yet Another Simple Backup Enabler

WPF, SQL Server

Windows application that automates staging a batch of files to be backed up to arbitrary removable media (e.g. optical discs - DVD, Blu-ray, etc.) and cataloging the result for later reference.

I made this because from what I can tell as of Q1 2011, the popular backup softwares (Acronis, Paragon, Yosemite, etc.) all have [serious issues with Blu-ray](http://forum.acronis.com/forum/14860).

Features:
  * Saves a "profile" which comprises all the desired folder path selections for a particular backup set.
  * The folder selection process is typical tree view based; including ability to include all subfolders or not.
  * Performs a simple "best fit" approach, according to the selected media size, to fill up each disc as much as possible before prompting for the next blank.
  * Stages files via simple copy and therefore the backups will be simple native files as well. I feel most data at rest is relatively compressed in its native form already these days. Doing it this way makes it easy to throw a disc into any system and retrieve files via simple copy. i.e. the lowest possible risk of any restoration issues.
  * Catalogs to SQL Server. Express works fine. Requires at least v2008 due to table valued parameters.
  * The catalog facilitates a few features:
    * Ready location of which disc a particular file/folder is stored on; including multiple copies of the same file for each time it was identified as updated (via file modified date).
    * **Incremental backup** - by comparing filesystem modified dates to what's in the database, only new/updated files are identified for each subsequent backup.

additional writeup here: [http://www.beejblog.com/2011/03/yasbe-open-source-code-incremental.html](http://www.beejblog.com/2011/03/yasbe-open-source-code-incremental.html)
