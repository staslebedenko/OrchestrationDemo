# OrchestrationDemo
Demo application for data processing with Azure Durable Functions. 

This demo is a part of the ".NET Fest 2019" talk "Practical serverless use cases in Azure with a trick or two".

https://www.youtube.com/watch?v=eHMBBUck24A&list=PLuOBDBq7MW72wvYB5l5pmwBYdF9bY3Ctx&index=35&t=0s


Durable orchestration with Zoltar 8-).

```bash

#----------------------------------------------------------------------------------
# Resource group
#----------------------------------------------------------------------------------

# subscription switch and check
subscriptionID=$(az account list --query "[?contains(name,'Microsoft')].[id]" -o tsv)
echo "Test subscription ID is = " $subscriptionID
az account set --subscription $subscriptionID
az account show


location=northeurope
postfix=$RANDOM

# resource group
groupName=AzureDurableDemo$postfix

az group create --name $groupName --location $location
#az group delete --name $groupName


#----------------------------------------------------------------------------------
# Storage account with Blob container
#----------------------------------------------------------------------------------

location=northeurope
accountSku=Standard_LRS
accountName=${groupName,,}
echo "accountName  = " $accountName

az storage account create --name $accountName --location $location --kind StorageV2 \
--resource-group $groupName --sku $accountSku --access-tier Hot  --https-only true

accountKey=$(az storage account keys list --resource-group $groupName --account-name $accountName --query "[0].value" | tr -d '"')
echo "storage account key = " $accountKey

storageAccConString="DefaultEndpointsProtocol=https;AccountName=$accountName;AccountKey=$accountKey;EndpointSuffix=core.windows.net"
echo "storage account connection string = " $storageAccConString

blobName=${groupName,,}

az storage container create --name $blobName \
--account-name $accountName --account-key $accountKey --public-access off

requestQueue=zoltar-requests
resultQueue=zoltar-results
errorQueue=zoltar-results

az storage queue create --name $requestQueue --account-key $accountKey \
--account-name $accountName --connection-string $storageAccConString

az storage queue create --name $resultQueue --account-key $accountKey \
--account-name $accountName --connection-string $storageAccConString

az storage queue create --name $errorQueue --account-key $accountKey \
--account-name $accountName --connection-string $storageAccConString

#----------------------------------------------------------------------------------
# Application insights instance
#----------------------------------------------------------------------------------

insightsName=${groupName,,}
echo "insightsName  = " $insightsName

# drop this command with ctrl+c after 3 minutes of execution
az resource create --resource-group $groupName --name $insightsName --resource-type "Microsoft.Insights/components" --location $location --properties '{"Application_Type":"web"}' --verbose

insightsKey=$(az resource show -g $groupName -n $insightsName --resource-type "Microsoft.Insights/components" --query properties.InstrumentationKey --o tsv) 
echo "Insights key = " $insightsKey


#----------------------------------------------------------------------------------
# Function app with consumption plan. Use KeyVault in production :)
#----------------------------------------------------------------------------------

runtime=dotnet
location=northeurope
applicationName=${groupName,,}
accountName=${groupName,,}
echo "applicationName  = " $applicationName

az functionapp create --resource-group $groupName \
--name $applicationName --storage-account $accountName --runtime $runtime \
--app-insights-key $insightsKey --consumption-plan-location $location

az functionapp deployment slot create --resource-group $groupName --name $applicationName --slot staging

az functionapp update --resource-group $groupName --name $applicationName --set dailyMemoryTimeQuota=400000

az functionapp identity assign --resource-group $groupName --name $applicationName

az functionapp config appsettings set --resource-group $groupName --name $applicationName --settings "MSDEPLOY_RENAME_LOCKED_FILES=1"

managedIdKey=$(az functionapp identity show --name $applicationName --resource-group $groupName --query principalId --o tsv)
echo "Managed Id key = " $managedIdKey

#----------------------------------------------------------------------------------
# Azure SQL Server and Serverless DB 1-4 cores and 32 Gb storage
#----------------------------------------------------------------------------------

location=northeurope
serverName=${groupName,,}
adminLogin=Admin$groupName
password=Sup3rStr0ng$groupName$postfix
databaseName=${groupName,,}
serverSku=S0
catalogCollation="SQL_Latin1_General_CP1_CI_AS"

az sql server create --name $serverName --resource-group $groupName --assign-identity \
--location $location --admin-user $adminLogin --admin-password $password

az sql db create --resource-group $groupName --server $serverName --name $databaseName \
--edition GeneralPurpose --family Gen5 --compute-model Serverless \
--auto-pause-delay 60 --capacity 4

outboundIps=$(az webapp show --resource-group $groupName --name $applicationName --query possibleOutboundIpAddresses --output tsv)
IFS=',' read -r -a ipArray <<< "$outboundIps"

for ip in "${ipArray[@]}"
do
echo "$ip add"
az sql server firewall-rule create --resource-group $groupName --server $serverName \
--name "WebApp$ip" --start-ip-address $ip --end-ip-address $ip
done

sqlClientType=ado.net

#TODO add Admin login and remove password, set to variable.
sqlConString=$(az sql db show-connection-string --name $databaseName --server $serverName --client $sqlClientType --o tsv)
sqlConString=${sqlConString/Password=<password>;}
sqlConString=${sqlConString/<username>/$adminLogin}
echo "SQL Connection string is = " $sqlConString

# on your PC run CMD as administrator, then execute following commands and reboot PC.
# just copy command output below to CMD and execute.

az functionapp config appsettings set --resource-group $groupName --name $applicationName --settings "SqlConnectionString=$sqlConString"
az functionapp config appsettings set --resource-group $groupName --name $applicationName --settings "SqlConnectionPassword=$password"
az functionapp config appsettings set --resource-group $groupName --name $applicationName --settings "StorageConnectionString=$storageAccConString"

#----------------------------------------------------------------------------------
# Key Vault with policies.
#----------------------------------------------------------------------------------

location=northeurope
keyVaultName=${groupName,,}
echo "keyVaultName  = " $keyVaultName

az keyvault create --name $keyVaultName --resource-group $groupName --location $location 

az keyvault set-policy --name $keyVaultName --object-id $managedIdKey \
--certificate-permissions get list --key-permissions get list --secret-permissions get list

az keyvault secret set --vault-name $keyVaultName --name FancySecret  --value 'SuperSecret'
az keyvault secret set --vault-name $keyVaultName --name SqlConnectionString  --value "$sqlConString"
az keyvault secret set --vault-name $keyVaultName --name SqlConnectionPassword  --value $password
az keyvault secret set --vault-name $keyVaultName --name StorageConnectionString  --value $storageAccConString

# on your PC run CMD as administrator, then execute following commands and reboot PC.
# just copy command output below to CMD and execute.
echo "setx APPINSIGHTS_INSTRUMENTATIONKEY "$insightsKey
echo "setx StorageConnectionString \""$storageAccConString\"
echo "setx SqlConnectionString \""$sqlConString\"
echo "setx SqlConnectionPassword "$password

```