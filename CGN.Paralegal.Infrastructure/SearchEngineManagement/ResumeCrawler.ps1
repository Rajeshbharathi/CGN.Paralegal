param ([string]$config)
[string]$scriptpath = Split-Path -parent $MyInvocation.MyCommand.Definition
$configfile = $scriptpath + "\vivisimo_instances_config.xml"
if (![string]::IsNullOrEmpty($config))
{
   $configfile = $config
}

# Load the config file from the same location as the script file
[xml]$instancesXml = Get-Content -Path $configfile

#Append the error to the error file
$errorfilename = "\CrawlerResumeLog.txt"
$errorfile = $scriptpath + $errorfilename
if ($instancesXml.config.errorfilepath)
{
  $errorfile =  $instancesXml.config.errorfilepath + $errorfilename
}

foreach( $instance in $instancesXml.config.instances.instance) 
{ 
	#default credentials
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
	
	$errorcontent = new-object System.Text.StringBuilder
	[void]$errorcontent.Append("`n*******************************************`n")
	[void]$errorcontent.Append("`n  Velocity Sever : ").Append($name).Append("   `n")
	[void]$errorcontent.Append("`n*******************************************`n")
	Add-Content -Path $errorfile -Value $errorcontent.ToString()	
	Write-Host $errorcontent.ToString()
	
	# construct the url to determine the list of collections using search-collection-list-xml API
	$url = "http://" + $name + ":" + $port + "/vivisimo/cgi-bin/velocity.exe?v.app=api-rest&v.username=" + $admin + "&v.password=" + $password

	$param = new-object System.Text.StringBuilder
	[void]$param.Append("v.function=search-collection-list-xml&v.indent=true&v.app=api-rest")
	$command = $param.ToString()	
	$bytes = [System.Text.Encoding]::ASCII.GetBytes($command)

	$req = [System.Net.WebRequest]::Create($url)
	$req.Method = "POST"
	$req.ContentLength = $bytes.Length
	$req.ContentType = "application/x-www-form-urlencoded"

	$stream = $req.GetRequestStream()
	$stream.Write($bytes,0,$bytes.Length)
	$stream.close()


	$resp = $req.GetResponse()
	$reader = new-object System.IO.StreamReader($resp.GetResponseStream())
	[string] $results = $reader.ReadToEnd()
	$reader.Close()		
	
	if ($results -like "*<exception*</exception>*")
	{
	        #In case of exception, log it
  		
		$currenttime = Get-Date
		$errorcontent = new-object System.Text.StringBuilder
		[void]$errorcontent.Append("`n").Append($currenttime).Append("`n")
		[void]$errorcontent.Append("-------------------------------------`n")
		[void]$errorcontent.Append("uri:").Append($url).Append("`n")
		[void]$errorcontent.Append($results)
		[void]$errorcontent.Append("`n-------------------------------------")
		Add-Content -Path $errorfile -Value $errorcontent.ToString()
	}
	else
	{	
		#if($results -ne "")
		if($results.Length -eq 0)
		{
			#If No response text s returned, then the server or its api is unreachable
			$errorcontent = new-object System.Text.StringBuilder
			[void]$errorcontent.Append("`nERROR - UNABLE TO REACH / ACCESS VELOCITY SERVER (or) API - ").Append($name).Append("`n")
			Add-Content -Path $errorfile -Value $errorcontent.ToString()
			Write-Host $errorcontent.ToString()
		}
		else
		{
			#Replace "-" with "0" to get rid of parsing issues
			$results = $results.Replace("vse-collections","vseocollections")
			$results = $results.Replace("vse-collection","vseocollection")
			[xml]$responseXml = $results

			$errorcontent = new-object System.Text.StringBuilder
			[void]$errorcontent.Append("`n-------------------------------------")
			Add-Content -Path $errorfile -Value $errorcontent.ToString()	

			foreach($collection in $responseXml.vseocollections.vseocollection) 
			{
			   $collectionName = $collection.name

			   #Lookup only for the collections whose name starts with Vault-
			   if($collectionName -match "Vault")
			   {								
				# construct the url to determine search collection status using search-collection-status API
				$url = "http://" + $name + ":" + $port + "/vivisimo/cgi-bin/velocity.exe?v.app=api-rest&v.username=" + $admin + "&v.password=" + $password

				$param = new-object System.Text.StringBuilder
				[void]$param.Append("v.function=search-collection-status&v.indent=true&collection=").Append($collectionName).Append("&subcollection=live&stale-ok=false&v.app=api-rest")
				$command = $param.ToString()	
				$bytes = [System.Text.Encoding]::ASCII.GetBytes($command)

				$req = [System.Net.WebRequest]::Create($url)
				$req.Method = "POST"
				$req.ContentLength = $bytes.Length
				$req.ContentType = "application/x-www-form-urlencoded"

				$stream = $req.GetRequestStream()
				$stream.Write($bytes,0,$bytes.Length)
				$stream.close()


				$resp = $req.GetResponse()
				$reader = new-object System.IO.StreamReader($resp.GetResponseStream())
				[string] $results = $reader.ReadToEnd()
				$reader.Close()		
				
				
				if ($results -like "*service-status=`"stopped`"*")
				{	
					#In case, crawler service is stopped, then resume the crawler service for the collection using search-collection-crawler-start API					
					$url = "http://" + $name + ":" + $port + "/vivisimo/cgi-bin/velocity.exe?v.app=api-rest&v.username=" + $admin + "&v.password=" + $password

					$param = new-object System.Text.StringBuilder
					[void]$param.Append("v.function=search-collection-crawler-start&v.indent=true&collection=").Append($collectionName).Append("&subcollection=live&type=resume&v.app=api-rest")
					$command = $param.ToString()	
					$bytes = [System.Text.Encoding]::ASCII.GetBytes($command)

					$req = [System.Net.WebRequest]::Create($url)
					$req.Method = "POST"
					$req.ContentLength = $bytes.Length
					$req.ContentType = "application/x-www-form-urlencoded"

					$stream = $req.GetRequestStream()
					$stream.Write($bytes,0,$bytes.Length)
					$stream.close()


					$resp = $req.GetResponse()
					$reader = new-object System.IO.StreamReader($resp.GetResponseStream())
					[string] $results = $reader.ReadToEnd()
					$reader.Close()	

					$currenttime = Get-Date
					$errorcontent = new-object System.Text.StringBuilder
					[void]$errorcontent.Append("`n").Append($currenttime).Append("`n")
					[void]$errorcontent.Append(" *** Matter name: ").Append($collectionName).Append(" *** Crawler State: RESUMED ").Append("`n")
					Add-Content -Path $errorfile -Value $errorcontent.ToString()
					Write-Host $errorcontent.ToString()
					
					#Sleep for a 2.5 seconds just to make sure velocity is not squeezed
					Start-Sleep -m 2500
				}
				else
				{					
					$currenttime = Get-Date
					$errorcontent = new-object System.Text.StringBuilder
					[void]$errorcontent.Append("`n").Append($currenttime).Append("`n")
					[void]$errorcontent.Append(" *** Matter name: ").Append($collectionName).Append(" *** Crawler State: RUNNING ").Append("`n")
					Add-Content -Path $errorfile -Value $errorcontent.ToString()	
					Write-Host $errorcontent.ToString()
				
				}
			    }
			}
			$errorcontent = new-object System.Text.StringBuilder
			[void]$errorcontent.Append("`n-------------------------------------")
			Add-Content -Path $errorfile -Value $errorcontent.ToString()		
				
		}

	}
}
# If all successful then exit from the script with exit code 0
#exit 0
$Host.SetShouldExit(0) 
exit 