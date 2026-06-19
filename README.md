## Run

```bash
dotnet build -c Release OrderBookTask
dotnet run -c Release --no-build --project OrderBookTask
```

The application reads `ticks.raw` and generates `ticks_result.csv`.

Optional arguments:

```bash
dotnet run -c Release --no-build --project OrderBookTask <input-file> <output-file>
```
