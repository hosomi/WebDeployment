# WebDeployment

## Usage

```cmd
Usage: WebDeployment.exe <publishSettings> <source>
```

### publishSettings:


example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<publishData>
  <publishProfile
    publishUrl="https://myhostname:8172/msdeploy.axd"
    msdeploySite="Default Web Site"
    destinationAppUrl="http://myhostname:80/"
    mySQLDBConnectionString=""
    SQLServerDBConnectionString=""
    profileName="Default Settings"
    publishMethod="MSDeploy"
    userName="myhostname\myusername" />
</publishData>
```


### source: deployment source

xxx.zip 




---