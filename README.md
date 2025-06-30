### Init

Para levantar el proyecto, primero [levante RabbitMQ con Docker](./rabbit_up.sh).

### Uso

```
dotnet run produce => Producir votos.
dotnet run consume => Consumir votos, mostrando los resultados finales.
dotnet run subscribe => Consumir votos en el momento que se hacen. Muestra resultados live.
```
