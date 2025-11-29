module BackupManager

open System
open System.IO

let private backupDir = "backups"

let ensureBackupDirectoryExists () : unit =
    if not (Directory.Exists(backupDir)) then
        Directory.CreateDirectory(backupDir) |> ignore

let createBackup (sourceFilePath: string) : string =
    ensureBackupDirectoryExists()
    
    if File.Exists(sourceFilePath) then
        let timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
        let fileName = Path.GetFileNameWithoutExtension(sourceFilePath)
        let extension = Path.GetExtension(sourceFilePath)
        let backupFileName = $"{fileName}_backup_{timestamp}{extension}"
        let backupPath = Path.Combine(backupDir, backupFileName)
        
        File.Copy(sourceFilePath, backupPath, true)
        backupPath
    else
        raise (FileNotFoundException($"File not found: {sourceFilePath}"))

let listBackups () : string list =
    ensureBackupDirectoryExists()
    
    if Directory.Exists(backupDir) then
        Directory.GetFiles(backupDir)
        |> Array.toList
    else
        []

let deleteBackup (backupPath: string) : unit =
    if File.Exists(backupPath) then
        File.Delete(backupPath)

let deleteAllBackups () : unit =
    ensureBackupDirectoryExists()
    
    listBackups()
    |> List.iter (fun backup -> File.Delete(backup))

let restoreFromBackup (backupPath: string) (targetPath: string) : unit =
    if File.Exists(backupPath) then
        File.Copy(backupPath, targetPath, true)
    else
        raise (FileNotFoundException($"Backup not found: {backupPath}"))
