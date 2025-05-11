metadata description = 'Creates a .NET Aspire dashboard instance.'
param containerAppEnvironmentName string

resource cae 'Microsoft.App/managedEnvironments@2024-10-02-preview' existing = {
  name: containerAppEnvironmentName
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  name: 'aspire-dashboard'
  parent: cae
  properties: {
    componentType: 'AspireDashboard'
  }
}
