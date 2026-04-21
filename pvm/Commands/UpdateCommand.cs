namespace pvm.Commands;

public class UpdateCommand : InstallCommand
{
    public UpdateCommand() : base("update", "Re-install/update PHP via Chocolatey")
    {
    }
}
