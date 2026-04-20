#!/bin/bash

cd "$(dirname "$0")/PolynomialHash.Benchmarks" || exit 1

echo "Running PolynomialHash benchmarks..."
dotnet run --configuration Release

echo "Benchmarks completed!"
