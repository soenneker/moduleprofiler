# ModuleProfiler

[![build status](https://ci.appveyor.com/api/projects/status/github/soenneker/moduleprofiler?svg=true)](https://ci.appveyor.com/project/soenneker/moduleprofiler)

A rudimentary ASP.NET profiler that measures performance of requests.

It consists of three projects: the module, a sample web app, and some basic tests. The module injects a small amount of HTML into the end of the page which outputs analysis of the request.

## Getting Started

A demo can be found [here](https://moduleprofiler.azurewebsites.net).

Once the module has been built, a PowerShell script (`InstallScript.ps1`, run as admin) has been included in the solution that adds the module to all IIS sites.

A web.config setting (`FeatureToggle_Interface`) can enable/disable the UI overlay.

## Construction Plan

### Design

* Project spec'd and outlined
* Solution structure and initialization
* README

### Calculations
* Total request time
* Module time
* Size of response body
* Min/Avg/Max response body sizes
* Number of assemblies
* Memory used during request
* Number of strings created

### Module
* Logging
* Write request to console and log
* Create Request model
* Statistics overlay UI
* Request injection

### Web
* Feature toggle for module

### Tests
* Calculations
* Overall module operation

### IIS installation script
* Check for module existence
* Find each site, iterate over them
  * Stop site
  * Copy module (`/bin`) to installation directory
  * Identify and digest web.config
  * Modify web.config
  * Start site

### Deployment
* Azure web app demo
* Appveyor setup