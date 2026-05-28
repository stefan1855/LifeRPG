using LifeRPG.Storage.Services;

if (args.Length != 1)
{
    Console.Error.WriteLine("Backup path argument is required.");
    return 1;
}

var service = new BackupService();
var result = service.RestoreFromBackup(args[0]);
Console.WriteLine(result);
return 0;
