# New-LocalUser -Name Scanner
$credetial = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name;
$dir = "$pwd\release"
$binPath = "$dir\Scanner.exe"

# Write-Host $dir
# Write-Host $credetial
# Write-Host $binPath
dotnet publish -c release -o release

$acl = Get-Acl $dir
$aclRuleArgs = $credetial, "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
$acl.SetAccessRule($accessRule)
$acl | Set-Acl $dir
New-Service -Name Scanner -BinaryPathName $binPath -Credential $credetial -Description "Netum Scanner Worker Service" -DisplayName "Scanner COM Service" -StartupType Automatic
Start-Service -Name Scanner

