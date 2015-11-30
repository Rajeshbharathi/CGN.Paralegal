param ([string]$mode, [string]$config)
if ($mode -eq "Y" -or $mode -eq "N")
{}
else
{
 Throw "Parameter missing: -mode Y/N"
}

[string]$scriptpath = Split-Path -parent $MyInvocation.MyCommand.Definition
$configfile = $scriptpath + "\vivisimo_instances_config.xm"
if (![string]::IsNullOrEmpty($config))
{
   $configfile = $config
}


# Load the config file from the same location as the script file
[xml]$instancesXml = Get-Content -Path $configfile
foreach( $instance in $instancesXml.config.instances.instance) 
{ 
    $name = "localhost"
	$port = "80"
	$admin = "lexisnexis"
	$password = "YAue.O22"
	
	if (![string]::IsNullOrEmpty($instance.name))
	{
      $name = $instance.name
	}
	if (![string]::IsNullOrEmpty($instance.port))
	{
      $port = $instance.port
	}
	if (![string]::IsNullOrEmpty($instance.admin))
	{
      $admin = $instance.admin
	} 
	if (![string]::IsNullOrEmpty($instance.password))
	{
      $password = $instance.password
	} 
	if ($mode -eq "Y")
	{
  		$pmode = "enable"
	} elseif ($mode -eq "N")
	{
  		$pmode = "disable" 
	}
	
	# construct the url to invoke
	$url = "http://" + $name + ":" + $port + "/vivisimo/cgi-bin/velocity.exe?v.function=search-collection-read-only-all&v.indent=true&v.app=api-rest&v.username=" + $admin + "&v.password=" + $password + "&mode=" + $pmode
	
	$req = [System.Net.WebRequest]::Create($url)
	$req.Method ="GET"
	$req.ContentLength = 0

	$resp = $req.GetResponse()
	$reader = new-object System.IO.StreamReader($resp.GetResponseStream())
	[string] $results = $reader.ReadToEnd()
	#Write-Host $results
	if ($results -like "*<exception*</exception>*")
	{
  		#Append the error to the error file
		$errorfilename = "\VivisimoError.txt"
		$errorfile = $scriptpath + $errorfilename
		if ($instancesXml.config.errorfilepath)
		{
		  $errorfile =  $instancesXml.config.errorfilepath + $errorfilename
		}
		$currenttime = Get-Date
		$errorcontent = new-object System.Text.StringBuilder
		[void]$errorcontent.Append("`n").Append($currenttime).Append("`n")
		[void]$errorcontent.Append("-------------------------------------`n")
		[void]$errorcontent.Append("uri:").Append($url).Append("`n")
		[void]$errorcontent.Append($results)
		[void]$errorcontent.Append("`n-------------------------------------")
		Add-Content -Path $errorfile -Value $errorcontent.ToString()
	}
}
# If all successful then exit from the script with exit code 0
#exit 0
$Host.SetShouldExit(0) 
exit 






