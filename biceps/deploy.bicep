//////////////////////////////////////////////////////////////////////
//// Parameters
//////////////////////////////////////////////////////////////////////

param location string = resourceGroup().location
param resourceNamePostfix string = uniqueString(resourceGroup().id)

param storageAccountName string = 'str${resourceNamePostfix}'

param functionAppName string = 'func-${resourceNamePostfix}'
param appInsightsName string = 'appi-${resourceNamePostfix}'
param appServicePlanName string = 'plan-${resourceNamePostfix}'

//////////////////////////////////////////////////////////////////////
//// Modules
//////////////////////////////////////////////////////////////////////

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    name: storageAccountName
  }
}

module functionApp 'modules/functions.bicep' = {
  name: 'functionApp'
  params: {
    storageAccountName: storage.outputs.name
    location: location
    appInsightsName: appInsightsName
    appServicePlanName: appServicePlanName
    functionAppName: functionAppName
  }
}

//////////////////////////////////////////////////////////////////////
//// Outputs
//////////////////////////////////////////////////////////////////////

output storageAccountName string = storage.outputs.name
output functionAppName string = functionApp.outputs.functionAppName
