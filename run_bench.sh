OUTPUT_PATH="${1:-BenchmarkDotNet.Artifacts/benchmark_results.txt}"
mkdir -p "$(dirname "$OUTPUT_PATH")"
cd src && dotnet run -c Release --project Tedd.TUI.Benchmarks/Tedd.TUI.Benchmarks.csproj --filter *ParseMouseSGR* > "../$OUTPUT_PATH"
