#!/bin/bash -e

# 変数を定義する
region='japaneast'    # デプロイ先のリージョン
resourceGroupName=$1  # デプロイ先のリソースグループ (スクリプトの引数から取得する)

# リソースグループを作成する
az group create \
    --location $region \
    --resource-group $resourceGroupName

# Azure リソースをデプロイする
outputs=($(az deployment group create \
            --resource-group $resourceGroupName \
            --template-file biceps/deploy.bicep \
            --query 'properties.outputs.*.value' \
            --output tsv))
storageAccountName=`echo ${outputs[0]}` # 文末の \r を削除する
functionAppName=${outputs[1]}

# openai_service-accounts.json ファイルから Functions が使用する Azure OpenAI Service アカウント情報を取得する
openAIServiceAccounts=$(sed -z 's/\n//g' openai_service_accounts.json)
openAIServiceAccounts=$(echo $openAIServiceAccounts | sed -e 's|"|\"|g')

# Azure Functions のアプリケーション設定を設定する
az functionapp config appsettings set \
    --resource-group $resourceGroupName \
    --name $functionAppName \
    --settings "OPENAI_SERVICE_ACCOUNTS=$openAIServiceAccounts"

# Azure Functions のアプリケーションをデプロイする
pushd functions
sleep 10 # Azure Functions App リソースの作成からコードデプロイが早すぎると「リソースが見つからない」エラーが発生する場合があるので、一時停止する
func azure functionapp publish $functionAppName --csharp
popd

# Azure Functions のエンドポイント(キー付き)を取得する
functionCode=`az functionapp function keys list \
    --resource-group $resourceGroupName \
    --name $functionAppName \
    --function-name 'Function' \
    --query "default" \
    --output tsv`
functionApiUri=`echo https://$functionAppName.azurewebsites.net/api/Function?code=$functionCode`

echo 'Azure Cognitive Search のスキルセット"#Microsoft.Skills.Custom.WebApiSkill"のURLとして以下のURLを使用することができます:'
echo $functionApiUri
echo '### Azure Cognitive Search のスキルセットでの使用例'
sed -e "s|{{CUSTOM_WEB_API_URI}}|$functionApiUri|;" cogsearch/skillset.json