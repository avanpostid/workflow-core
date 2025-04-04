dotnet pack -c Release -o ./
dotnet nuget push "*.nupkg" -s https://packages.avanpost.ru/repository/nuget-hosted
del *.nupkg