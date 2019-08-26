# ServiceControl.Monitoring

A standalone service gathering metrics from endpoints and providing them with an http endpoint.

## Releasing

To release a new version of ServiceControl.Monitoring:

1. Merge changes according to the branching strategy
1. Run the Deploy build in TeamCity to push a copy of the latest [`Packaging.ServiceControl.Monitoring`](https://www.nuget.org/packages/Particular.PlatformSample.ServiceControl.Monitoring/) package
1. Update the [ServiceControl Installer Packaging project](https://github.com/Particular/ServiceControl/blob/master/src/ServiceControlInstaller.Packaging/ServiceControlInstaller.Packaging.csproj#L21) in a PR
1. Release ServiceControl
1. Check whether the [Platform Sample](https://github.com/Particular/Particular.PlatformSample) needs to be updated
