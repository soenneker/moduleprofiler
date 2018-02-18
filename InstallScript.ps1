Set-ExecutionPolicy Unrestricted

Import-Module WebAdministration

function Log($msg)
{
	Write-Output $msg
	sleep 1
}

function StopSite($siteName) 
{
	Log("Stopping " + $siteName + "...")
	Stop-WebSite $siteName;
}

function StartSite($siteName) 
{
	Log("Starting " + $siteName + "...")
	Start-WebSite $siteName;
}

function AddFeatureToggle($configPath)
{
	$cfg = [xml](gc $configPath)
	$appSettings = $cfg.configuration["appSettings"]

	if (!($appSettings.add | Where-Object { $_.key -eq 'FeatureToggle_Interface' }))
	{
		Log("Adding feature toggle to web.config...")

		$as = $cfg.CreateElement("add")
		$as.SetAttribute("key", "FeatureToggle_Interface")
		$as.SetAttribute("value", "true")
		$appSettings.AppendChild($as)
		$cfg.Save($configPath);
	}
}

function AddModule($configPath)
{
	$cfg = [xml](gc $configPath)
	$systemweb = $cfg.configuration["system.webServer"]

	Write-Output $systemweb.modules

	if ($systemweb.modules -eq $null)
	{
		Log("Adding module to web.config...")
		$as = $cfg.CreateElement("modules")
		$systemweb.AppendChild($as)
	}

	if ($systemweb.modules.add -eq $null -Or !($systemweb.modules.add | Where-Object { $_.name -eq 'RequestCaptureModule' }))
	{
		Log("Adding HTTPModule to web.config...")
		$modules = $systemweb["modules"]
		$as = $cfg.CreateElement("add")
		$as.SetAttribute("name", "RequestCaptureModule")
		$as.SetAttribute("type", "ModuleProfiler.Module.RequestCaptureModule")
		$modules.AppendChild($as)
		$cfg.Save($configPath);
	}
}

Log('Installing ModuleProfiler into all IIS sites...')

$currentLocation = (Get-Location).path

$modulePath = $currentLocation + '\ModuleProfiler.Module\bin\Debug\*'

if (!(Test-Path $modulePath -PathType Leaf))
{
	Log('ModuleProfiler not found, be sure to build the solution first. exiting...')
	exit
}

$sites = Get-ChildItem -Path IIS:\Sites

foreach ($site in $sites) 
{
	$binPath = $site.PhysicalPath + '\bin\'

	$configPath = $site.PhysicalPath + '\web.config'

	if (!(Test-Path $binPath))
	{
		Log("Path not found: ($binPath) continuing...")
		continue
	}

	StopSite($site.Name)

	AddFeatureToggle($configPath)

	AddModule($configPath)

	Log("Installing into " + $site.Name + " ($binPath)")

	Copy-Item $modulePath -Destination $sitePath -Recurse

	StartSite($site.Name)

	Log("")
}

Log("exiting...")