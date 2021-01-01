# About

This shows how to use the [HttpListener class](https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=netframework-4.7.2) to create an HTTP.sys listener in a simple C# application.

# Usage

Execute the following commands in a administrative PowerShell session:

```powershell
# create local user.
$username = 'test'
$password = 'HeyH0Password!'
$passwordSecureString = ConvertTo-SecureString $password -AsPlainText -Force
New-LocalUser `
    -AccountNeverExpires `
    -PasswordNeverExpires `
    -Name $username `
    -Password $passwordSecureString

# add URL ACL.
# NB the address MUST contain the port number.
# NB the address MUST end in a forward slash.
# NB localhost addresses do not need url acl permissions.
netsh http show iplisten
netsh http add urlacl "url=http://+:80/hello/" "user=$username" listen=yes
netsh http show urlacl

# execute the application as the local user.
runas "/user:$username" powershell
cd bin\Release
.\HelloHttpListener.exe "http://+:80/hello/"

# in another shell, test accessing the application.
Invoke-RestMethod http://localhost/hello

# delete the URL ACL and local user.
netsh http delete urlacl "url=http://+:80/hello/"
Remove-LocalUser $username
```
